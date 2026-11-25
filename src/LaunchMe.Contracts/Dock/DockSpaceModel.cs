namespace LaunchMe.Contracts.Dock;

public sealed class DockSpaceModel
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string DisplayId { get; set; } = string.Empty;
    public string Name { get; set; } = "Default Dock";
    public List<DockItem> Items { get; set; } = [];
    public List<string> ActiveWidgets { get; set; } = ["SystemStats", "MusicCompact", "ToolBar"];
}

public interface IDockSpaceService : IDisposable
{
    IReadOnlyList<DockSpaceModel> GetDockSpaces();
    DockSpaceModel? GetActiveDockSpace(string displayId);
    void SetActiveDockSpace(string displayId, string dockSpaceId);
    void SaveDockSpace(DockSpaceModel model);
    void NextDockSpace(string displayId);
    void PreviousDockSpace(string displayId);
    event EventHandler? DockSpacesChanged;
}
