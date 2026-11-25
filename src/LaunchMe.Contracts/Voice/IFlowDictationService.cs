namespace LaunchMe.Contracts.Voice;

public enum DictationMode
{
    PushToTalk,
    HandsFreeToggle,
    Off
}

public record SpeechTranscriptEventArgs(string Text, bool IsFinal, double Confidence);

public interface IFlowDictationService
{
    event EventHandler<float[]> AudioLevelsUpdated; // 9-band reactive frequency levels for Pill
    event EventHandler<SpeechTranscriptEventArgs> TranscriptReceived;
    event EventHandler<bool> StateChanged;

    bool IsListening { get; }
    DictationMode Mode { get; }

    void StartDictation(DictationMode mode = DictationMode.PushToTalk);
    void StopDictation();
    void CancelDictation();
    void AddVocabularyWord(string original, string replacement);
}
