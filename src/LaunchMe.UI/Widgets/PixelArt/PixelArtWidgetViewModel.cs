using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.UI.Theming;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Models;
using Microsoft.UI.Xaml.Media;
using Windows.UI;

namespace LaunchMe.UI.Widgets.PixelArt;

public enum PixelArtPreset
{
    Slime,
    Cat,
    Fox,
    Owl,
    Frog,
    Ghost,
    Robot,
    Duck,
    Penguin,
    Bunny,
    Corgi,
    Dragon,
    Astronaut,
    Mushroom,
    Crystal,
    Campfire,
    Coffee,
    Sakura
}

public sealed record PixelArtPresetVisual(
    string DisplayName,
    string PrimaryHex,
    string SecondaryHex,
    string[] Frames);

public sealed class PixelArtWidgetViewModel : IWidgetViewModel
{
    private const int FrameIntervalMs = 350;

    private static readonly IReadOnlyDictionary<PixelArtPreset, PixelArtPresetVisual> PresetCatalog =
        new Dictionary<PixelArtPreset, PixelArtPresetVisual>
        {
            [PixelArtPreset.Slime] = new("Slime", "#38BDF8", "#BAE6FD", [
                "..11...111.121.11111.111.",
                ".111...1111121111111.111.",
                "..11...111.121.111111111.",
                "..11...111.111.12111.111."
            ]),
            [PixelArtPreset.Cat] = new("Cat", "#F59E0B", "#FEF3C7", [
                "1...1111111212111111.111.",
                "1...1111111111112121.111.",
                "1...1111111212111111..11.",
                "1...1111111211111111.111."
            ]),
            [PixelArtPreset.Fox] = new("Fox", "#EA580C", "#FFF7ED", [
                "1...1111111212111111..2..",
                "1...11111112121.111...2..",
                "1...1111111112111111..2..",
                "1...1111111212111111.22.."
            ]),
            [PixelArtPreset.Owl] = new("Owl", "#A855F7", "#FDE047", [
                "1...1.111.12121.111..1.1.",
                "1...1.111.11111.111..1.1.",
                ".1.1..111.12121.111..1.1.",
                "1...1.111.1212111111.1.1."
            ]),
            [PixelArtPreset.Frog] = new("Frog", "#22C55E", "#DCFCE7", [
                ".2.2.111111212111111.1.1.",
                ".2.2.111111111111111.1.1.",
                ".....12121111111111111.11",
                ".2.2.1111112121.111..1.1."
            ]),
            [PixelArtPreset.Ghost] = new("Ghost", "#E2E8F0", "#38BDF8", [
                ".111.1111112121111111.1.1",
                ".111.111111212111111.1.1.",
                ".111.1111111212111111.1.1",
                "..111.11111212111111.1.1."
            ]),
            [PixelArtPreset.Robot] = new("Robot", "#64748B", "#22D3EE", [
                "..2...111.12121.111..1.1.",
                "..1...111.12121.111..1.1.",
                "..2...111.11111.111..1.1.",
                "..2..1111112121.111.11.11"
            ]),
            [PixelArtPreset.Duck] = new("Duck", "#FACC15", "#FB923C", [
                ".111.1121.11112.111...11.",
                ".111.1121.11122.111...11.",
                "..111.112111112.111...11.",
                ".111.1111.11112.111..1.1."
            ]),
            [PixelArtPreset.Penguin] = new("Penguin", "#334155", "#F8FAFC", [
                ".111.121211121112221.1.1.",
                ".111.1212111211122211...1",
                ".111.111111121112221.1.1.",
                ".111.121211222112221.1.1."
            ]),
            [PixelArtPreset.Bunny] = new("Bunny", "#F472B6", "#FDF2F8", [
                ".1.1..1.1..111.12121.111.",
                ".1.1..1.1..111.11111.111.",
                "1...1.1.1..111.12121.111.",
                ".1.1..111.12121.111..1.1."
            ]),
            [PixelArtPreset.Corgi] = new("Corgi", "#F97316", "#FEF3C7", [
                "1...1111111212112221.1.1.",
                "1...1111111111112221.1.1.",
                "1...11111112121122211...1",
                ".1.1.111111212112221.1.1."
            ]),
            [PixelArtPreset.Dragon] = new("Dragon", "#EF4444", "#FBBF24", [
                "2...2111111212111111.1.2.",
                "2...211111121211121111.22",
                "1...111111121211111121.2.",
                "2...2111111111111111.1.2."
            ]),
            [PixelArtPreset.Astronaut] = new("Astronaut", "#F8FAFC", "#38BDF8", [
                ".111.1222112221.111..1.1.",
                ".111.1212112221.111..1.1.",
                ".111.122211222111111.1.1.",
                "..111.2221.2221.111..1.1."
            ]),
            [PixelArtPreset.Mushroom] = new("Mushroom", "#EF4444", "#FEF2F2", [
                ".111.1212111111..22...22.",
                ".111.1121112121..22...22.",
                ".111.1212111111..22..222.",
                "..1111212111111..22...22."
            ]),
            [PixelArtPreset.Crystal] = new("Crystal", "#06B6D4", "#CFFAFE", [
                "..2...121.11211.111...1..",
                "..1...212.12121.111...1..",
                "..2...111.12221.121...2..",
                "..1...121.11211.121...1.."
            ]),
            [PixelArtPreset.Campfire] = new("Campfire", "#F97316", "#FDE047", [
                "..2...121.11211.111.22222",
                ".2....121.12121.111.22222",
                "...2..121.112111111.22222",
                "..2...212.12221.111.22222"
            ]),
            [PixelArtPreset.Coffee] = new("Coffee", "#D97706", "#FDE68A", [
                ".2.2...2..1111.11111.111.",
                "2...2..2..1111.11111.111.",
                ".2...2....1111.11111.111.",
                "..2...2...1111.11111.111."
            ]),
            [PixelArtPreset.Sakura] = new("Sakura", "#F472B6", "#FCE7F3", [
                ".1.1.11211.121.11211.1.1.",
                "1...1.121.12221.121.1...1",
                ".1.1.11211.222.11211.1.1.",
                ".1.1.12111.121.11121.1.1."
            ])
        };

    private static readonly IReadOnlyList<PixelArtPreset> AllPresets = Enum.GetValues<PixelArtPreset>();

    private readonly Timer? _frameTimer;
    private readonly object _lock = new();

    private PixelArtPreset _selectedPreset = PixelArtPreset.Slime;
    private int _currentFrameIndex;
    private int _frameCounter;
    private bool _isActive = true;
    private bool _isTimerRunning;
    private bool _disposed;

    public string WidgetId { get; } = "widget-pixel-art";
    public string Title => "Pixel Art";
    public WidgetFamily Family => WidgetFamily.PixelArt;
    public int FrameCount => 4;

    public IReadOnlyList<PixelArtPreset> Presets => AllPresets;

    public bool IsActive
    {
        get => _isActive;
        set { if (_isActive != value) { _isActive = value; OnPropertyChanged(); } }
    }

    public PixelArtPreset SelectedPreset
    {
        get => _selectedPreset;
        set
        {
            if (_selectedPreset != value)
            {
                _selectedPreset = value;
                _currentFrameIndex = 0;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PresetDisplayName));
                OnPropertyChanged(nameof(AccentHex));
                OnPropertyChanged(nameof(SecondaryHex));
                OnPropertyChanged(nameof(AccentBrush));
                OnPropertyChanged(nameof(CurrentFrameIndex));
                OnPropertyChanged(nameof(FrameStatusText));
            }
        }
    }

    public int CurrentFrameIndex
    {
        get => _currentFrameIndex;
        private set
        {
            if (_currentFrameIndex != value)
            {
                _currentFrameIndex = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FrameStatusText));
            }
        }
    }

    public int FrameCounter
    {
        get => _frameCounter;
        private set
        {
            if (_frameCounter != value)
            {
                _frameCounter = value;
                OnPropertyChanged();
            }
        }
    }

    public string PresetDisplayName => PresetCatalog[SelectedPreset].DisplayName;
    public string AccentHex => PresetCatalog[SelectedPreset].PrimaryHex;
    public string SecondaryHex => PresetCatalog[SelectedPreset].SecondaryHex;
    public SolidColorBrush? AccentBrush => ThemeVisualState.TryCreateBrush(LiquidGlassTintService.ParseHexColor(AccentHex, Color.FromArgb(255, 56, 189, 248)));
    public string FrameStatusText => $"Frame {CurrentFrameIndex + 1}/{FrameCount}";
    public bool IsTimerRunning => _isTimerRunning;

    public ICommand NextPresetCommand { get; }
    public ICommand PreviousPresetCommand { get; }
    public ICommand SelectPresetCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    public PixelArtWidgetViewModel(bool startActive = true)
    {
        NextPresetCommand = new RelayCommand(NextPreset);
        PreviousPresetCommand = new RelayCommand(PreviousPreset);
        SelectPresetCommand = new RelayCommand<PixelArtPreset>(preset => SelectedPreset = preset);

        _frameTimer = new Timer(OnFrameTick, null, Timeout.Infinite, Timeout.Infinite);
        if (startActive)
        {
            StartTimer();
        }
    }

    public void NextPreset()
    {
        int idx = ((int)SelectedPreset + 1) % AllPresets.Count;
        SelectedPreset = AllPresets[idx];
    }

    public void PreviousPreset()
    {
        int idx = ((int)SelectedPreset - 1 + AllPresets.Count) % AllPresets.Count;
        SelectedPreset = AllPresets[idx];
    }

    public void AdvanceFrame()
    {
        lock (_lock)
        {
            if (_disposed) return;
            CurrentFrameIndex = (_currentFrameIndex + 1) % FrameCount;
            FrameCounter++;
        }
    }

    public IReadOnlyList<string?> GetCurrentFrameColors()
    {
        var visual = PresetCatalog[SelectedPreset];
        string mask = visual.Frames[CurrentFrameIndex % visual.Frames.Length];
        var cells = new string?[25];
        for (int i = 0; i < 25 && i < mask.Length; i++)
        {
            cells[i] = mask[i] switch
            {
                '1' => visual.PrimaryHex,
                '2' => visual.SecondaryHex,
                _ => null
            };
        }
        return cells;
    }

    private void OnFrameTick(object? state)
    {
        if (!_isActive || !_isTimerRunning || _disposed) return;
        AdvanceFrame();
    }

    private void StartTimer()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _isTimerRunning = true;
            _frameTimer?.Change(FrameIntervalMs, FrameIntervalMs);
        }
    }

    private void StopTimer()
    {
        lock (_lock)
        {
            _isTimerRunning = false;
            if (_disposed) return;
            _frameTimer?.Change(Timeout.Infinite, Timeout.Infinite);
        }
    }

    public void OnLauncherHidden()
    {
        // Pause-on-Hide GPU Law: immediately stop pixel-art animation timer
        StopTimer();
    }

    public void OnLauncherShown()
    {
        if (IsActive)
        {
            StartTimer();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
            _isTimerRunning = false;
            _frameTimer?.Change(Timeout.Infinite, Timeout.Infinite);
            _frameTimer?.Dispose();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
