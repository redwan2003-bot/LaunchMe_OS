namespace LaunchMe.Contracts.Spaces;

/// <summary>
/// Registers and unregisters Windows shell file associations for portable .launchmetheme packages.
/// </summary>
public interface IThemeAssociationService
{
    bool IsRegistered { get; }
    void RegisterFileAssociation(string executablePath);
    void UnregisterFileAssociation();
}
