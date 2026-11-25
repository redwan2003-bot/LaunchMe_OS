using LaunchMe.Contracts.Dock;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Platform.Win32.Metrics;
using LaunchMe.UI.ViewModels;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class DockViewModelTests : IDisposable
{
    private sealed class MockDockSpaceService : IDockSpaceService
    {
        public int NextCallCount { get; private set; }
        public int PrevCallCount { get; private set; }
        public event EventHandler? DockSpacesChanged;

        public IReadOnlyList<DockSpaceModel> GetDockSpaces() => [];
        public DockSpaceModel? GetActiveDockSpace(string displayId) => new() { DisplayId = displayId, Name = "Test Dock Space" };
        public void SetActiveDockSpace(string displayId, string dockSpaceId) { }
        public void SaveDockSpace(DockSpaceModel model) => DockSpacesChanged?.Invoke(this, EventArgs.Empty);
        public void NextDockSpace(string displayId) { NextCallCount++; DockSpacesChanged?.Invoke(this, EventArgs.Empty); }
        public void PreviousDockSpace(string displayId) { PrevCallCount++; DockSpacesChanged?.Invoke(this, EventArgs.Empty); }
        public void Dispose() { }
    }

    private sealed class MockAmbientSoundService : IAmbientSoundService
    {
        public AmbientSoundPreset CurrentPreset { get; private set; } = AmbientSoundPreset.None;
        public bool IsPlaying { get; private set; }
        public double Volume { get; set; } = 0.5;
        public event EventHandler? StateChanged;

        public void Play(AmbientSoundPreset preset)
        {
            CurrentPreset = preset;
            IsPlaying = true;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Pause()
        {
            IsPlaying = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Stop()
        {
            CurrentPreset = AmbientSoundPreset.None;
            IsPlaying = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() { }
    }

    private sealed class MockAppLauncher : IAppLauncherService
    {
        public string? LastLaunched { get; private set; }
        public bool LaunchTarget(string targetPath, string? arguments = null)
        {
            LastLaunched = targetPath;
            return true;
        }
        public Task<bool> LaunchAppAsync(LaunchMe.Contracts.Indexing.AppItem app, CancellationToken ct = default) => Task.FromResult(true);
    }

    private readonly MockDockSpaceService _mockDockSpace = new();
    private readonly MockAmbientSoundService _mockAmbient = new();
    private readonly MockAppLauncher _mockLauncher = new();
    private readonly DockViewModel _vm;

    public DockViewModelTests()
    {
        _vm = new DockViewModel(
            dockShellService: null,
            dockSpaceService: _mockDockSpace,
            metricsService: null,
            ambientSoundService: _mockAmbient,
            appLauncherService: _mockLauncher);
    }

    public void Dispose()
    {
        _vm.Dispose();
        _mockDockSpace.Dispose();
        _mockAmbient.Dispose();
    }

    [Fact]
    public void ToggleFloating_FlipsState()
    {
        Assert.False(_vm.IsFloating);
        _vm.ToggleFloating();
        Assert.True(_vm.IsFloating);
        _vm.ToggleFloating();
        Assert.False(_vm.IsFloating);
    }

    [Fact]
    public void NextAndPrevSpace_DelegatesToSpaceService()
    {
        _vm.NextSpace();
        Assert.Equal(1, _mockDockSpace.NextCallCount);

        _vm.PreviousSpace();
        Assert.Equal(1, _mockDockSpace.PrevCallCount);
    }

    [Fact]
    public void ToggleAmbientSound_TogglesPlayback()
    {
        Assert.False(_vm.IsAmbientPlaying);
        _vm.ToggleAmbientSound();
        Assert.True(_vm.IsAmbientPlaying);
        Assert.True(_mockAmbient.IsPlaying);

        _vm.ToggleAmbientSound();
        Assert.False(_vm.IsAmbientPlaying);
        Assert.False(_mockAmbient.IsPlaying);
    }

    [Fact]
    public void CycleAmbientPreset_CyclesThroughSoundscapes()
    {
        _vm.CycleAmbientPreset();
        Assert.Equal(AmbientSoundPreset.Rain, _vm.CurrentAmbientPreset);

        _vm.CycleAmbientPreset();
        Assert.Equal(AmbientSoundPreset.OceanWaves, _vm.CurrentAmbientPreset);

        _vm.CycleAmbientPreset();
        Assert.Equal(AmbientSoundPreset.Forest, _vm.CurrentAmbientPreset);
    }

    [Fact]
    public void LaunchDockItem_LaunchesTarget()
    {
        var item = new DockItem { Title = "Notepad", TargetPath = "notepad.exe" };
        _vm.LaunchDockItem(item);
        Assert.Equal("notepad.exe", _mockLauncher.LastLaunched);
    }
}
