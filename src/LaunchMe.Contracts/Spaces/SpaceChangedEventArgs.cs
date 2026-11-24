namespace LaunchMe.Contracts.Spaces;

public sealed class SpaceChangedEventArgs : EventArgs
{
    public string PreviousSpaceId { get; }
    public string NewSpaceId { get; }
    public SpaceModel? ActiveSpace { get; }

    public SpaceChangedEventArgs(string previousSpaceId, string newSpaceId, SpaceModel? activeSpace = null)
    {
        PreviousSpaceId = previousSpaceId;
        NewSpaceId = newSpaceId;
        ActiveSpace = activeSpace;
    }
}
