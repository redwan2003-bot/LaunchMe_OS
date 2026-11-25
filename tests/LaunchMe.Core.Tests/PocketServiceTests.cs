using LaunchMe.Contracts.Notch;
using LaunchMe.Core.Notch;
using Xunit;

namespace LaunchMe.Core.Tests;

public sealed class PocketServiceTests : IDisposable
{
    private readonly string _tempFile;

    public PocketServiceTests()
    {
        _tempFile = Path.Combine(Path.GetTempPath(), $"pocket_test_{Guid.NewGuid():N}.json");
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
    public void AddFile_ValidPath_AddsItemAndFiresEvent()
    {
        using var service = new PocketService(_tempFile);
        bool eventFired = false;
        service.ItemsChanged += (_, _) => eventFired = true;

        string dummyFile = Path.Combine(Path.GetTempPath(), $"dummy_{Guid.NewGuid():N}.txt");
        File.WriteAllText(dummyFile, "Hello Pocket Shelf");

        try
        {
            var item = service.AddFile(dummyFile);

            Assert.NotNull(item);
            Assert.Equal(dummyFile, item.FilePath);
            Assert.True(item.FileSizeBytes > 0);
            Assert.True(eventFired);

            var items = service.GetItems();
            Assert.Single(items);
            Assert.Equal(item.Id, items[0].Id);
        }
        finally
        {
            File.Delete(dummyFile);
        }
    }

    [Fact]
    public void RemoveFile_ExistingId_RemovesAndFiresEvent()
    {
        using var service = new PocketService(_tempFile);
        var item = service.AddFile("C:\\test\\document.pdf");
        Assert.Single(service.GetItems());

        bool eventFired = false;
        service.ItemsChanged += (_, _) => eventFired = true;

        bool removed = service.RemoveFile(item.Id);
        Assert.True(removed);
        Assert.True(eventFired);
        Assert.Empty(service.GetItems());
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        using var service = new PocketService(_tempFile);
        service.AddFile("C:\\test\\1.png");
        service.AddFile("C:\\test\\2.png");
        Assert.Equal(2, service.GetItems().Count);

        service.Clear();
        Assert.Empty(service.GetItems());
    }

    [Fact]
    public void Persistence_ReloadsFromDisk()
    {
        using (var service1 = new PocketService(_tempFile))
        {
            service1.AddFile("C:\\test\\persisted.docx");
        }

        using (var service2 = new PocketService(_tempFile))
        {
            var items = service2.GetItems();
            Assert.Single(items);
            Assert.Equal("persisted.docx", items[0].FileName);
        }
    }
}
