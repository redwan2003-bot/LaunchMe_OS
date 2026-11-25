using System.Text.RegularExpressions;
using LaunchMe.Contracts.Voice;

namespace LaunchMe.Core.Voice;

/// <summary>
/// Service coordinating push-to-talk speech dictation, audio level analysis, and smart text injection.
/// </summary>
public class FlowDictationService : IFlowDictationService, IDisposable
{
    private readonly IKeystrokeInjector? _injector;
    private readonly Dictionary<string, string> _vocabulary = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    private bool _isListening;
    private DictationMode _mode = DictationMode.Off;
    private bool _disposed;

    public bool IsListening => _isListening;
    public DictationMode Mode => _mode;

    public event EventHandler<float[]>? AudioLevelsUpdated;
    public event EventHandler<SpeechTranscriptEventArgs>? TranscriptReceived;
    public event EventHandler<bool>? StateChanged;

    public FlowDictationService(IKeystrokeInjector? injector = null)
    {
        _injector = injector;

        // Default vocabulary substitutions
        AddVocabularyWord("launch me", "LaunchMe");
        AddVocabularyWord("launchme win", "LaunchMe-Win");
        AddVocabularyWord("dot net", ".NET");
        AddVocabularyWord("c sharp", "C#");
    }

    public void StartDictation(DictationMode mode = DictationMode.PushToTalk)
    {
        lock (_lock)
        {
            if (_isListening)
            {
                return;
            }

            _isListening = true;
            _mode = mode;
            StateChanged?.Invoke(this, true);

            // Emit initial level
            AudioLevelsUpdated?.Invoke(this, new float[9]);
        }
    }

    public void StopDictation()
    {
        lock (_lock)
        {
            if (!_isListening)
            {
                return;
            }

            _isListening = false;
            _mode = DictationMode.Off;
            StateChanged?.Invoke(this, false);
            AudioLevelsUpdated?.Invoke(this, new float[9]);
        }
    }

    public void CancelDictation()
    {
        lock (_lock)
        {
            _isListening = false;
            _mode = DictationMode.Off;
            StateChanged?.Invoke(this, false);
            AudioLevelsUpdated?.Invoke(this, new float[9]);
        }
    }

    public void AddVocabularyWord(string original, string replacement)
    {
        lock (_lock)
        {
            _vocabulary[original] = replacement;
        }
    }

    public void FeedSpeechHypothesis(string rawText, bool isFinal, double confidence = 1.0)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            return;
        }

        string cleaned = CleanupTranscript(rawText);

        TranscriptReceived?.Invoke(this, new SpeechTranscriptEventArgs(cleaned, isFinal, confidence));

        if (isFinal && _isListening)
        {
            _injector?.InjectText(cleaned);
        }
    }

    public void FeedAudioLevels(float[] bands)
    {
        if (!_isListening || bands == null || bands.Length != 9)
        {
            return;
        }

        AudioLevelsUpdated?.Invoke(this, bands);
    }

    public string CleanupTranscript(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        string text = input.Trim();

        // 1. Remove filler words (um, uh, er, ah)
        text = Regex.Replace(text, @"\b(um|uh|er|ah)\b,?\s*", "", RegexOptions.IgnoreCase);

        // 2. Vocabulary replacement
        lock (_lock)
        {
            foreach (var (orig, repl) in _vocabulary)
            {
                text = Regex.Replace(text, $@"\b{Regex.Escape(orig)}\b", repl, RegexOptions.IgnoreCase);
            }
        }

        // 3. Capitalize first letter
        if (text.Length > 0 && char.IsLower(text[0]))
        {
            text = char.ToUpper(text[0]) + text[1..];
        }

        return text.Trim();
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            StopDictation();
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }
}
