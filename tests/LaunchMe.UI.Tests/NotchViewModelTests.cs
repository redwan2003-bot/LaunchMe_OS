using LaunchMe.Contracts.Notch;
using LaunchMe.UI.ViewModels;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class NotchViewModelTests : IDisposable
{
    private sealed class MockNotchWindowService : INotchWindowService
    {
        public bool ExclusionApplied { get; private set; }
        public bool GameActive { get; set; }

        public void ApplyScreenCaptureExclusion(nint hwnd, bool exclude) => ExclusionApplied = exclude;
        public bool IsFullscreenGameActive() => GameActive;
        public void Dispose() { }
    }

    private sealed class MockPocketService : IPocketService
    {
        private readonly List<PocketItem> _items = [];
        public event EventHandler? ItemsChanged;

        public IReadOnlyList<PocketItem> GetItems() => _items;

        public PocketItem AddFile(string filePath)
        {
            var item = new PocketItem(Guid.NewGuid().ToString(), filePath, Path.GetFileName(filePath), 1024, DateTime.UtcNow);
            _items.Add(item);
            ItemsChanged?.Invoke(this, EventArgs.Empty);
            return item;
        }

        public bool RemoveFile(string id)
        {
            var removed = _items.RemoveAll(i => i.Id == id) > 0;
            if (removed) ItemsChanged?.Invoke(this, EventArgs.Empty);
            return removed;
        }

        public void Clear()
        {
            _items.Clear();
            ItemsChanged?.Invoke(this, EventArgs.Empty);
        }

        public void Dispose() { }
    }

    private readonly MockNotchWindowService _mockNotch = new();
    private readonly MockPocketService _mockPocket = new();
    private readonly NotchViewModel _vm;

    public NotchViewModelTests()
    {
        _vm = new NotchViewModel(_mockNotch, _mockPocket);
    }

    public void Dispose()
    {
        _vm.Dispose();
        _mockNotch.Dispose();
        _mockPocket.Dispose();
    }

    [Fact]
    public void ToggleExpand_ChangesDimensions()
    {
        Assert.False(_vm.IsExpanded);
        Assert.Equal(420, _vm.WidthPx);
        Assert.Equal(36, _vm.HeightPx);

        _vm.ToggleExpand();
        Assert.True(_vm.IsExpanded);
        Assert.Equal(640, _vm.WidthPx);
        Assert.Equal(180, _vm.HeightPx);

        _vm.ToggleExpand();
        Assert.False(_vm.IsExpanded);
        Assert.Equal(420, _vm.WidthPx);
        Assert.Equal(36, _vm.HeightPx);
    }

    [Fact]
    public void Pocket_AddAndRemoveItems_UpdatesCollection()
    {
        _vm.AddPocketFile("C:\\files\\document.pdf");
        Assert.Single(_vm.PocketItems);
        Assert.Equal("document.pdf", _vm.PocketItems[0].FileName);

        _vm.RemovePocketFile(_vm.PocketItems[0].Id);
        Assert.Empty(_vm.PocketItems);
    }

    [Fact]
    public void Timer_StartPauseReset_ControlsCountdown()
    {
        _vm.StartTimer(10);
        Assert.True(_vm.IsTimerRunning);
        Assert.Equal("10:00", _vm.TimerDisplay);

        _vm.PauseTimer();
        Assert.False(_vm.IsTimerRunning);

        _vm.ResetTimer();
        Assert.False(_vm.IsTimerRunning);
        Assert.Equal("25:00", _vm.TimerDisplay);
    }

    [Fact]
    public void CheckFullscreenGame_ReflectsServiceState()
    {
        _mockNotch.GameActive = true;
        _vm.CheckFullscreenGame();
        Assert.True(_vm.IsHiddenByFullscreenGame);

        _mockNotch.GameActive = false;
        _vm.CheckFullscreenGame();
        Assert.False(_vm.IsHiddenByFullscreenGame);
    }

    [Fact]
    public void ScreenCaptureExclusion_CallsService()
    {
        _vm.ApplyScreenCaptureExclusion(new nint(12345));
        Assert.True(_mockNotch.ExclusionApplied);
    }
}
