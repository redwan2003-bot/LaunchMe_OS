using LaunchMe.Contracts.Dock;
using LaunchMe.Core.Dock;
using Xunit;

namespace LaunchMe.Core.Tests;

public sealed class DockSpaceServiceTests : IDisposable
{
    private readonly string _tempFile;

    public DockSpaceServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"dock_space_test_{Guid.NewGuid():N}.json");
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_tempFile))
            {
                File.Delete(_tempFile);
            }
        }
        catch
        {
        }
    }

    [Fact]
    public void GetActiveDockSpace_CreatesDefaultWhenEmpty()
    {
        using var service = new DockSpaceService(_tempFile);
        var space = service.GetActiveDockSpace("Display_1");

        Assert.NotNull(space);
        Assert.Equal("Display_1", space.DisplayId);
        Assert.NotEmpty(space.Items);
        Assert.Contains("SystemStats", space.ActiveWidgets);
    }

    [Fact]
    public void NextAndPreviousDockSpace_CyclesCircularly()
    {
        using var service = new DockSpaceService(_tempFile);
        var space1 = new DockSpaceModel { Id = "s1", DisplayId = "Display_1", Name = "Space 1" };
        var space2 = new DockSpaceModel { Id = "s2", DisplayId = "Display_1", Name = "Space 2" };
        var space3 = new DockSpaceModel { Id = "s3", DisplayId = "Display_1", Name = "Space 3" };

        service.SaveDockSpace(space1);
        service.SaveDockSpace(space2);
        service.SaveDockSpace(space3);

        service.SetActiveDockSpace("Display_1", "s1");
        Assert.Equal("s1", service.GetActiveDockSpace("Display_1")?.Id);

        service.NextDockSpace("Display_1");
        Assert.Equal("s2", service.GetActiveDockSpace("Display_1")?.Id);

        service.NextDockSpace("Display_1");
        Assert.Equal("s3", service.GetActiveDockSpace("Display_1")?.Id);

        service.NextDockSpace("Display_1");
        Assert.Equal("s1", service.GetActiveDockSpace("Display_1")?.Id);

        service.PreviousDockSpace("Display_1");
        Assert.Equal("s3", service.GetActiveDockSpace("Display_1")?.Id);
    }

    [Fact]
    public void MultiDisplay_MaintainsSeparateActiveSpaces()
    {
        using var service = new DockSpaceService(_tempFile);
        var disp1Space = service.GetActiveDockSpace("Display_A");
        var disp2Space = service.GetActiveDockSpace("Display_B");

        Assert.NotNull(disp1Space);
        Assert.NotNull(disp2Space);
        Assert.NotEqual(disp1Space.Id, disp2Space.Id);
    }
}
