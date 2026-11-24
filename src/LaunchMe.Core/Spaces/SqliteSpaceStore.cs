using System.IO.Compression;
using System.Text.Json;
using LaunchMe.Contracts.Dock;
using LaunchMe.Contracts.Launcher;
using LaunchMe.Contracts.Notch;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Contracts.Workflows;
using Microsoft.Data.Sqlite;

namespace LaunchMe.Core.Spaces;

public sealed class SqliteSpaceStore : ISpaceStore, IDisposable
{
    private readonly string _connectionString;
    private readonly string _baseDirectory;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _initialized;

    public event EventHandler<SpaceChangedEventArgs>? SpaceChanged;

    public SqliteSpaceStore(string? dbPath = null)
    {
        _baseDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "LaunchMe");

        Directory.CreateDirectory(_baseDirectory);

        var finalDbPath = dbPath ?? Path.Combine(_baseDirectory, "launchme.db");
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = finalDbPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            Cache = SqliteCacheMode.Shared
        };
        _connectionString = builder.ToString();
    }

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized) return;

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized) return;

            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS spaces (
                    space_id TEXT PRIMARY KEY NOT NULL,
                    name TEXT NOT NULL,
                    is_active INTEGER NOT NULL DEFAULT 0,
                    json_data TEXT NOT NULL,
                    updated_at TEXT NOT NULL
                );
                CREATE INDEX IF NOT EXISTS idx_spaces_active ON spaces (is_active);
                """;
            await command.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            // Check if seeding is necessary
            using var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM spaces;";
            var count = Convert.ToInt64(await countCmd.ExecuteScalarAsync(ct).ConfigureAwait(false));

            if (count == 0)
            {
                var defaultSpace = new SpaceModel
                {
                    SpaceId = "default-space",
                    Name = "Default Space",
                    IsDefault = true,
                    Schedule = new SpaceSchedule
                    {
                        Days = ["Mon", "Tue", "Wed", "Thu", "Fri"],
                        Start = "09:00",
                        End = "18:00"
                    },
                    Theme = new ThemeSettings
                    {
                        Style = "LiquidGlass",
                        Mode = "Tinted",
                        TintHex = "#1E40AF",
                        Intensity = 0.65,
                        TitleColor = "Auto"
                    },
                    Wallpaper = new WallpaperSettings
                    {
                        Type = WallpaperType.Static,
                        Asset = string.Empty,
                        Blur = 0.0,
                        PauseOnHide = true
                    },
                    Launcher = new LauncherSettings
                    {
                        ClassicView = true,
                        FreeLayout = true,
                        HideTitles = false,
                        IconScale = 1.0,
                        FolderGrid = "4x6",
                        Pages = []
                    },
                    Notch = new NotchSettings
                    {
                        Enabled = true,
                        Bg = "BlackGlass",
                        OpenMode = "Hover",
                        HideInGames = true,
                        ExcludeCapture = true,
                        Widgets = ["Music:Compact", "Timer:Compact"]
                    },
                    Dock = new DockSettings
                    {
                        PerDisplay = true,
                        Floating = false,
                        MaxWindowMode = "ReserveWorkArea",
                        Widgets = ["SystemStats", "MusicCompact", "ToolBar"]
                    },
                    Workflows = []
                };

                var json = JsonSerializer.Serialize(defaultSpace);
                using var seedCmd = connection.CreateCommand();
                seedCmd.CommandText = """
                    INSERT INTO spaces (space_id, name, is_active, json_data, updated_at)
                    VALUES ($id, $name, 1, $json, $updated);
                    """;
                seedCmd.Parameters.AddWithValue("$id", defaultSpace.SpaceId);
                seedCmd.Parameters.AddWithValue("$name", defaultSpace.Name);
                seedCmd.Parameters.AddWithValue("$json", json);
                seedCmd.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
                await seedCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
            }

            _initialized = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<SpaceModel>> GetAllSpacesAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT json_data FROM spaces ORDER BY name ASC;";

        var list = new List<SpaceModel>();
        using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        while (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var json = reader.GetString(0);
            var model = JsonSerializer.Deserialize<SpaceModel>(json);
            if (model != null)
            {
                list.Add(model);
            }
        }

        return list;
    }

    public async Task<SpaceModel?> GetActiveSpaceAsync(CancellationToken ct = default)
    {
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(ct).ConfigureAwait(false);

        using var command = connection.CreateCommand();
        command.CommandText = "SELECT json_data FROM spaces WHERE is_active = 1 LIMIT 1;";

        using var reader = await command.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await reader.ReadAsync(ct).ConfigureAwait(false))
        {
            var json = reader.GetString(0);
            return JsonSerializer.Deserialize<SpaceModel>(json);
        }

        // Fallback to first available space
        using var fallbackCmd = connection.CreateCommand();
        fallbackCmd.CommandText = "SELECT json_data FROM spaces LIMIT 1;";
        using var fallbackReader = await fallbackCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
        if (await fallbackReader.ReadAsync(ct).ConfigureAwait(false))
        {
            var json = fallbackReader.GetString(0);
            return JsonSerializer.Deserialize<SpaceModel>(json);
        }

        return null;
    }

    public async Task SwitchSpaceAsync(string spaceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var transaction = connection.BeginTransaction();

            // Find current active space id
            string prevSpaceId = string.Empty;
            using var getPrevCmd = connection.CreateCommand();
            getPrevCmd.Transaction = transaction;
            getPrevCmd.CommandText = "SELECT space_id FROM spaces WHERE is_active = 1 LIMIT 1;";
            var prevObj = await getPrevCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (prevObj != null)
            {
                prevSpaceId = prevObj.ToString() ?? string.Empty;
            }

            // Verify target exists and fetch its json
            using var fetchCmd = connection.CreateCommand();
            fetchCmd.Transaction = transaction;
            fetchCmd.CommandText = "SELECT json_data FROM spaces WHERE space_id = $id;";
            fetchCmd.Parameters.AddWithValue("$id", spaceId);
            var targetJsonObj = await fetchCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (targetJsonObj == null)
            {
                throw new KeyNotFoundException($"Space with ID '{spaceId}' was not found.");
            }

            var targetSpace = JsonSerializer.Deserialize<SpaceModel>(targetJsonObj.ToString()!)
                ?? throw new InvalidOperationException("Failed to deserialize target space.");

            // Clear previous active flag
            using var resetCmd = connection.CreateCommand();
            resetCmd.Transaction = transaction;
            resetCmd.CommandText = "UPDATE spaces SET is_active = 0;";
            await resetCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            // Set new active flag and update json_data
            targetSpace.IsDefault = true;
            var updatedJson = JsonSerializer.Serialize(targetSpace);

            using var activateCmd = connection.CreateCommand();
            activateCmd.Transaction = transaction;
            activateCmd.CommandText = """
                UPDATE spaces 
                SET is_active = 1, json_data = $json, updated_at = $updated 
                WHERE space_id = $id;
                """;
            activateCmd.Parameters.AddWithValue("$id", spaceId);
            activateCmd.Parameters.AddWithValue("$json", updatedJson);
            activateCmd.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
            await activateCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            transaction.Commit();

            SpaceChanged?.Invoke(this, new SpaceChangedEventArgs(prevSpaceId, spaceId, targetSpace));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SaveSpaceAsync(SpaceModel space, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(space);
        ArgumentException.ThrowIfNullOrWhiteSpace(space.SpaceId);
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            var json = JsonSerializer.Serialize(space);
            using var cmd = connection.CreateCommand();
            cmd.CommandText = """
                INSERT INTO spaces (space_id, name, is_active, json_data, updated_at)
                VALUES ($id, $name, $active, $json, $updated)
                ON CONFLICT(space_id) DO UPDATE SET
                    name = excluded.name,
                    is_active = excluded.is_active,
                    json_data = excluded.json_data,
                    updated_at = excluded.updated_at;
                """;
            cmd.Parameters.AddWithValue("$id", space.SpaceId);
            cmd.Parameters.AddWithValue("$name", space.Name);
            cmd.Parameters.AddWithValue("$active", space.IsDefault ? 1 : 0);
            cmd.Parameters.AddWithValue("$json", json);
            cmd.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
            await cmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task DeleteSpaceAsync(string spaceId, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync(ct).ConfigureAwait(false);

            using var transaction = connection.BeginTransaction();

            using var checkCmd = connection.CreateCommand();
            checkCmd.Transaction = transaction;
            checkCmd.CommandText = "SELECT is_active FROM spaces WHERE space_id = $id;";
            checkCmd.Parameters.AddWithValue("$id", spaceId);
            var activeObj = await checkCmd.ExecuteScalarAsync(ct).ConfigureAwait(false);
            if (activeObj == null) return;

            var wasActive = Convert.ToInt64(activeObj) == 1;

            using var delCmd = connection.CreateCommand();
            delCmd.Transaction = transaction;
            delCmd.CommandText = "DELETE FROM spaces WHERE space_id = $id;";
            delCmd.Parameters.AddWithValue("$id", spaceId);
            await delCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);

            if (wasActive)
            {
                using var promoteCmd = connection.CreateCommand();
                promoteCmd.Transaction = transaction;
                promoteCmd.CommandText = "SELECT space_id, json_data FROM spaces LIMIT 1;";
                using var reader = await promoteCmd.ExecuteReaderAsync(ct).ConfigureAwait(false);
                if (await reader.ReadAsync(ct).ConfigureAwait(false))
                {
                    var newActiveId = reader.GetString(0);
                    var newActiveJson = reader.GetString(1);
                    reader.Close();

                    var model = JsonSerializer.Deserialize<SpaceModel>(newActiveJson);
                    if (model != null)
                    {
                        model.IsDefault = true;
                        var updatedJson = JsonSerializer.Serialize(model);

                        using var updCmd = connection.CreateCommand();
                        updCmd.Transaction = transaction;
                        updCmd.CommandText = """
                            UPDATE spaces 
                            SET is_active = 1, json_data = $json, updated_at = $updated 
                            WHERE space_id = $id;
                            """;
                        updCmd.Parameters.AddWithValue("$id", newActiveId);
                        updCmd.Parameters.AddWithValue("$json", updatedJson);
                        updCmd.Parameters.AddWithValue("$updated", DateTimeOffset.UtcNow.ToString("O"));
                        await updCmd.ExecuteNonQueryAsync(ct).ConfigureAwait(false);
                    }
                }
            }

            transaction.Commit();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task ExportThemeAsync(string spaceId, string outputZipPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(spaceId);
        ArgumentException.ThrowIfNullOrWhiteSpace(outputZipPath);

        var spaces = await GetAllSpacesAsync(ct).ConfigureAwait(false);
        var space = spaces.FirstOrDefault(s => s.SpaceId.Equals(spaceId, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Space with ID '{spaceId}' not found.");

        var dir = Path.GetDirectoryName(outputZipPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        if (File.Exists(outputZipPath))
        {
            File.Delete(outputZipPath);
        }

        using var zipStream = new FileStream(outputZipPath, FileMode.Create, FileAccess.Write, FileShare.None);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, leaveOpen: false);

        var manifestEntry = archive.CreateEntry("manifest.json", CompressionLevel.Optimal);
        using var manifestStream = manifestEntry.Open();
        await JsonSerializer.SerializeAsync(manifestStream, space, new JsonSerializerOptions { WriteIndented = true }, ct).ConfigureAwait(false);

        // Bundle local wallpaper if present
        if (!string.IsNullOrWhiteSpace(space.Wallpaper.Asset) && File.Exists(space.Wallpaper.Asset))
        {
            var fileName = Path.GetFileName(space.Wallpaper.Asset);
            var assetEntry = archive.CreateEntry($"assets/{fileName}", CompressionLevel.Optimal);
            using var assetStream = assetEntry.Open();
            using var srcStream = File.OpenRead(space.Wallpaper.Asset);
            await srcStream.CopyToAsync(assetStream, ct).ConfigureAwait(false);
        }
    }

    public async Task<SpaceModel> ImportThemeAsync(string themeZipPath, CancellationToken ct = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(themeZipPath);
        if (!File.Exists(themeZipPath))
        {
            throw new FileNotFoundException("Theme zip file was not found.", themeZipPath);
        }

        using var zipStream = File.OpenRead(themeZipPath);
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read, leaveOpen: false);

        var manifestEntry = archive.GetEntry("manifest.json")
            ?? throw new InvalidDataException("Archive missing manifest.json");

        using var manifestStream = manifestEntry.Open();
        var importedSpace = await JsonSerializer.DeserializeAsync<SpaceModel>(manifestStream, cancellationToken: ct).ConfigureAwait(false)
            ?? throw new InvalidDataException("Failed to parse manifest.json from theme archive.");

        // Assign a fresh ID to avoid collisions
        importedSpace.SpaceId = Guid.NewGuid().ToString("N");
        importedSpace.IsDefault = false;

        // Unpack local assets safely with strict ZipSlip defense
        var assetsDir = Path.GetFullPath(Path.Combine(_baseDirectory, "Assets", importedSpace.SpaceId));
        var assetsDirPrefix = assetsDir.EndsWith(Path.DirectorySeparatorChar) ? assetsDir : assetsDir + Path.DirectorySeparatorChar;

        foreach (var entry in archive.Entries.Where(e => e.FullName.StartsWith("assets/", StringComparison.OrdinalIgnoreCase)))
        {
            if (string.IsNullOrEmpty(entry.Name)) continue;

            // ZipSlip protection: extract only the terminal filename, reject path traversal sequences
            var safeFileName = Path.GetFileName(entry.Name);
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Contains("..")) continue;

            var targetPath = Path.GetFullPath(Path.Combine(assetsDir, safeFileName));
            if (!targetPath.StartsWith(assetsDirPrefix, StringComparison.OrdinalIgnoreCase))
            {
                // Reject path traversal attempt
                continue;
            }

            Directory.CreateDirectory(assetsDir);
            using var entryStream = entry.Open();
            using var destStream = File.Create(targetPath);
            await entryStream.CopyToAsync(destStream, ct).ConfigureAwait(false);

            if (importedSpace.Wallpaper.Asset.EndsWith(safeFileName, StringComparison.OrdinalIgnoreCase))
            {
                importedSpace.Wallpaper.Asset = targetPath;
            }
        }

        await SaveSpaceAsync(importedSpace, ct).ConfigureAwait(false);
        return importedSpace;
    }

    public void Dispose()
    {
        _lock.Dispose();
    }
}
