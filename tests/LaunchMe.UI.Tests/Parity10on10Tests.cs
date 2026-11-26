using FluentAssertions;
using LaunchMe.Contracts.Clipboard;
using LaunchMe.Contracts.Indexing;
using LaunchMe.Contracts.Media;
using LaunchMe.Contracts.Search;
using LaunchMe.Contracts.Spaces;
using LaunchMe.UI.ViewModels;
using LaunchMe.UI.Widgets.Music;
using LaunchMe.UI.Widgets.Weather;
using Microsoft.UI.Xaml;
using Moq;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class Parity10on10Tests
{
    [Fact]
    public void Notch_BluetoothToastAndDrawerWidgetReorder_WorkCleanly()
    {
        using var vm = new NotchViewModel();
        vm.ShowBluetoothToast("AirPods Pro", 85);
        vm.BluetoothToastVisibility.Should().Be(Visibility.Visible);
        vm.BluetoothToastText.Should().Be("AirPods Pro • 85%");

        vm.DismissBluetoothToast();
        vm.BluetoothToastVisibility.Should().Be(Visibility.Collapsed);

        string firstId = vm.DrawerWidgets[0].Id;
        vm.MoveDrawerWidget(0, 2);
        vm.DrawerWidgets[2].Id.Should().Be(firstId);

        vm.ToggleDrawerWidgetSize(firstId);
        vm.DrawerWidgets[2].Size.Should().Be("Large");
    }

    [Fact]
    public void Dock_ToolBarActions_PopulateAndTriggerEvent()
    {
        using var dock = new DockViewModel();
        dock.ToolBarActions.Should().HaveCountGreaterThanOrEqualTo(4);

        DockToolBarAction? triggered = null;
        dock.ToolBarActionTriggered += (_, a) => triggered = a;

        dock.ExecuteToolBarActionCommand.Execute(dock.ToolBarActions[0]);
        triggered.Should().Be(dock.ToolBarActions[0]);
    }

    [Fact]
    public void WeatherWidget_SupportsAll9StylesAndCycling()
    {
        Enum.GetValues<WeatherStyle>().Should().HaveCount(9);
        using var vm = new WeatherWidgetViewModel(startActive: false);
        vm.Style.Should().Be(WeatherStyle.Forecast);

        vm.CycleStyleCommand.Execute(null);
        vm.Style.Should().Be(WeatherStyle.Panel);
    }

    [Fact]
    public void MusicWidget_SupportsAll6StylesAndCycling()
    {
        Enum.GetValues<MusicWidgetStyle>().Should().HaveCount(6);
        var media = new Mock<IMediaSessionService>();
        using var vm = new MusicWidgetViewModel(media.Object, startActive: false);
        vm.Style.Should().Be(MusicWidgetStyle.Minimal);

        vm.CycleStyleCommand.Execute(null);
        vm.Style.Should().Be(MusicWidgetStyle.FullCover);
    }

    [Fact]
    public void Launcher_FreeSlotMove_FolderDensity_HexTint_AndCleanUninstall()
    {
        var indexer = new Mock<IAppIndexer>();
        var apps = new List<AppItem>
        {
            new() { Id = "app-1", Name = "Alpha", ExecutablePath = "C:\\a.exe", Categories = ["Utilities"] },
            new() { Id = "app-2", Name = "Beta", ExecutablePath = "C:\\b.exe", Categories = ["Utilities"] },
            new() { Id = "app-3", Name = "Gamma", ExecutablePath = "C:\\c.exe", Categories = ["Utilities"] }
        };
        indexer.Setup(x => x.AllApps).Returns(apps);
        indexer.Setup(x => x.SearchApps(It.IsAny<string>())).Returns(apps);

        var spaceStore = new Mock<ISpaceStore>();
        spaceStore.Setup(x => x.GetAllSpacesAsync(It.IsAny<CancellationToken>())).ReturnsAsync([]);

        using var launcher = new LauncherViewModel(
            indexer.Object,
            Mock.Of<IFileSearchService>(),
            Mock.Of<IClipboardMonitor>(),
            Mock.Of<ICalculatorService>(),
            Mock.Of<IEmojiService>(),
            spaceStore.Object);

        launcher.LoadClassicView();
        launcher.MoveItemToSlot("app-3", 0).Should().BeTrue();
        launcher.Pages[0].Items[0].Id.Should().Be("app-3");

        launcher.CustomHexTint = "#00F0FF";
        launcher.ApplyCustomHexTintCommand.Execute(null);
        launcher.CustomHexTint.Should().Be("#00F0FF");

        // Folder density switching
        var folderVm = new LauncherFolderViewModel("Tools", FolderGridDensity.Grid4x6);
        launcher.ExecuteItem(new LauncherItemViewModel(folderVm.Id, folderVm.Name, "\uE8B7", LauncherItemType.Folder, folderVm));
        launcher.SetActiveFolderGridDensity("5x8");
        launcher.ActiveFolderColumns.Should().Be(8);

        // Clean Uninstall with temp trace folder
        string tempTrace = Path.Combine(Path.GetTempPath(), $"launchme-trace-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempTrace);
        File.WriteAllText(Path.Combine(tempTrace, "state.json"), "{}");

        launcher.CleanUninstallApp("app-1", tempTrace).Should().BeTrue();
        Directory.Exists(tempTrace).Should().BeFalse();
        indexer.Verify(x => x.HideApp("app-1", true), Times.Once);
    }
}
