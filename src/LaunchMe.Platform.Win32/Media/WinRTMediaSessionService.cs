using System.IO;
using Windows.Media.Control;
using Windows.Storage.Streams;
using LaunchMe.Contracts.Media;

namespace LaunchMe.Platform.Win32.Media;

/// <summary>
/// Native WinRT implementation of IMediaSessionService using
/// GlobalSystemMediaTransportControlsSessionManager (GSMTC).
/// </summary>
public sealed class WinRTMediaSessionService : IMediaSessionService
{
    private readonly IGsmtcAdapter _adapter;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private bool _disposed;

    public event EventHandler<MediaTrackInfo>? TrackChanged;

    public WinRTMediaSessionService() : this(new SystemGsmtcAdapter())
    {
    }

    public WinRTMediaSessionService(IGsmtcAdapter adapter)
    {
        _adapter = adapter ?? throw new ArgumentNullException(nameof(adapter));
        _adapter.TrackChanged += OnAdapterTrackChanged;
    }

    private void OnAdapterTrackChanged(object? sender, MediaTrackInfo track)
    {
        if (_disposed) return;
        TrackChanged?.Invoke(this, track);
    }

    public async Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            return await _adapter.GetCurrentTrackAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PlayPauseAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _adapter.PlayPauseAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task NextAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _adapter.NextAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task PreviousAsync(CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _adapter.PreviousAsync(ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken ct = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        await _lock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            await _adapter.SeekAsync(position, ct).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        _adapter.TrackChanged -= OnAdapterTrackChanged;
        _adapter.Dispose();
        _lock.Dispose();
    }
}

/// <summary>
/// Abstraction over the WinRT GlobalSystemMediaTransportControlsSessionManager to enable testability.
/// </summary>
public interface IGsmtcAdapter : IDisposable
{
    event EventHandler<MediaTrackInfo>? TrackChanged;
    Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default);
    Task PlayPauseAsync(CancellationToken ct = default);
    Task NextAsync(CancellationToken ct = default);
    Task PreviousAsync(CancellationToken ct = default);
    Task SeekAsync(TimeSpan position, CancellationToken ct = default);
}

/// <summary>
/// Real WinRT GSMTC implementation connecting to Windows.Media.Control.
/// </summary>
internal sealed class SystemGsmtcAdapter : IGsmtcAdapter
{
    private GlobalSystemMediaTransportControlsSessionManager? _manager;
    private GlobalSystemMediaTransportControlsSession? _currentSession;
    private readonly SemaphoreSlim _initLock = new(1, 1);
    private bool _initialized;
    private bool _disposed;

    public event EventHandler<MediaTrackInfo>? TrackChanged;

    private async Task EnsureInitializedAsync(CancellationToken ct)
    {
        if (_initialized || _disposed) return;

        await _initLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (_initialized || _disposed) return;

            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
                if (_manager != null)
                {
                    _manager.CurrentSessionChanged += OnCurrentSessionChanged;
                    AttachSession(_manager.GetCurrentSession());
                }
            }
            catch
            {
                // In non-interactive or unsupported host environments, gracefully handle initialization failure
            }

            _initialized = true;
        }
        finally
        {
            _initLock.Release();
        }
    }

    private void OnCurrentSessionChanged(
        GlobalSystemMediaTransportControlsSessionManager sender,
        CurrentSessionChangedEventArgs args)
    {
        if (_disposed) return;

        AttachSession(sender.GetCurrentSession());
        _ = NotifyTrackChangedAsync();
    }

    private void AttachSession(GlobalSystemMediaTransportControlsSession? session)
    {
        if (_currentSession != null)
        {
            try
            {
                _currentSession.MediaPropertiesChanged -= OnMediaPropertiesChanged;
                _currentSession.PlaybackInfoChanged -= OnPlaybackInfoChanged;
                _currentSession.TimelinePropertiesChanged -= OnTimelinePropertiesChanged;
            }
            catch
            {
            }
        }

        _currentSession = session;

        if (_currentSession != null)
        {
            _currentSession.MediaPropertiesChanged += OnMediaPropertiesChanged;
            _currentSession.PlaybackInfoChanged += OnPlaybackInfoChanged;
            _currentSession.TimelinePropertiesChanged += OnTimelinePropertiesChanged;
        }
    }

    private void OnMediaPropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        MediaPropertiesChangedEventArgs args)
    {
        _ = NotifyTrackChangedAsync();
    }

    private void OnPlaybackInfoChanged(
        GlobalSystemMediaTransportControlsSession sender,
        PlaybackInfoChangedEventArgs args)
    {
        _ = NotifyTrackChangedAsync();
    }

    private void OnTimelinePropertiesChanged(
        GlobalSystemMediaTransportControlsSession sender,
        TimelinePropertiesChangedEventArgs args)
    {
        _ = NotifyTrackChangedAsync();
    }

    private async Task NotifyTrackChangedAsync()
    {
        if (_disposed) return;
        try
        {
            var track = await BuildTrackInfoAsync(_currentSession, CancellationToken.None).ConfigureAwait(false);
            if (track != null)
            {
                TrackChanged?.Invoke(this, track);
            }
        }
        catch
        {
        }
    }

    public async Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default)
    {
        if (_disposed) return null;
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        return await BuildTrackInfoAsync(_currentSession, ct).ConfigureAwait(false);
    }

    private static async Task<MediaTrackInfo?> BuildTrackInfoAsync(
        GlobalSystemMediaTransportControlsSession? session,
        CancellationToken ct)
    {
        if (session == null) return null;

        try
        {
            var mediaProperties = await session.TryGetMediaPropertiesAsync();
            var playbackInfo = session.GetPlaybackInfo();
            var timeline = session.GetTimelineProperties();

            string title = mediaProperties?.Title ?? string.Empty;
            string artist = mediaProperties?.Artist ?? string.Empty;
            string album = mediaProperties?.AlbumTitle ?? string.Empty;
            string sourceApp = session.SourceAppUserModelId ?? string.Empty;

            bool isPlaying = playbackInfo?.PlaybackStatus ==
                             GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing;

            TimeSpan position = timeline?.Position ?? TimeSpan.Zero;
            TimeSpan duration = timeline?.EndTime ?? TimeSpan.Zero;

            string? thumbnailBase64 = null;
            if (mediaProperties?.Thumbnail != null)
            {
                try
                {
                    using var stream = await mediaProperties.Thumbnail.OpenReadAsync();
                    using var netStream = stream.AsStreamForRead();
                    using var ms = new MemoryStream();
                    await netStream.CopyToAsync(ms, ct).ConfigureAwait(false);
                    thumbnailBase64 = Convert.ToBase64String(ms.ToArray());
                }
                catch
                {
                    thumbnailBase64 = null;
                }
            }

            return new MediaTrackInfo
            {
                Title = title,
                Artist = artist,
                AlbumTitle = album,
                SourceAppId = sourceApp,
                IsPlaying = isPlaying,
                Position = position,
                Duration = duration,
                ThumbnailBase64 = thumbnailBase64
            };
        }
        catch
        {
            return null;
        }
    }

    public async Task PlayPauseAsync(CancellationToken ct = default)
    {
        if (_disposed) return;
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryTogglePlayPauseAsync();
            }
            catch
            {
            }
        }
    }

    public async Task NextAsync(CancellationToken ct = default)
    {
        if (_disposed) return;
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipNextAsync();
            }
            catch
            {
            }
        }
    }

    public async Task PreviousAsync(CancellationToken ct = default)
    {
        if (_disposed) return;
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TrySkipPreviousAsync();
            }
            catch
            {
            }
        }
    }

    public async Task SeekAsync(TimeSpan position, CancellationToken ct = default)
    {
        if (_disposed) return;
        await EnsureInitializedAsync(ct).ConfigureAwait(false);

        if (_currentSession != null)
        {
            try
            {
                await _currentSession.TryChangePlaybackPositionAsync(position.Ticks);
            }
            catch
            {
            }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_manager != null)
        {
            try
            {
                _manager.CurrentSessionChanged -= OnCurrentSessionChanged;
            }
            catch
            {
            }
        }

        AttachSession(null);
        _initLock.Dispose();
    }
}
