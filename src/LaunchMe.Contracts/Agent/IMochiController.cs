namespace LaunchMe.Contracts.Agent;

public enum MochiMood
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

public interface IMochiController
{
    MochiMood CurrentMood { get; }
    (double PupilX, double PupilY) PupilOffset { get; }

    void SetMood(MochiMood mood);
    void UpdatePupils(double cursorX, double cursorY, double notchCenterX, double notchCenterY);
    void Poke();

    event EventHandler<MochiMood> MoodChanged;
}
