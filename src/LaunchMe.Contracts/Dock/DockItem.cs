namespace LaunchMe.Contracts.Dock;

public sealed class DockItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Title { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public bool IsRunning { get; set; }
}
