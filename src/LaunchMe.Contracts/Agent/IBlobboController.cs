namespace LaunchMe.Contracts.Agent;

public enum BlobboMood
{
    Idle,
    Listening,
    Thinking,
    Poked,
    Dizzy,
    Sleeping,
    Dancing,
    Alert
}

/// <summary>
/// Controls the living Snoot HUD pet Blobbo's emotional state and cursor-tracking pupil physics.
/// </summary>
public interface IBlobboController
{
    BlobboMood CurrentMood { get; }
    (double PupilX, double PupilY) PupilOffset { get; }

    void SetMood(BlobboMood mood);
    void UpdatePupils(double cursorX, double cursorY, double snootCenterX, double snootCenterY);
    void Poke();

    event EventHandler<BlobboMood> MoodChanged;
}
