using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Voice;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Voice;

/// <summary>
/// ViewModel coordinating the floating 9-dot reactive audio waveform pill.
/// Enforces Pause-on-Hide GPU Law: stops animations and timers when inactive.
/// </summary>
public class FlowPillViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly IFlowDictationService _dictation;
    private float[] _audioLevels = new float[9];
    private string _lastTranscript = string.Empty;
    private bool _disposed;

    public FlowPillViewModel(IFlowDictationService dictation)
    {
        _dictation = dictation;
        _dictation.AudioLevelsUpdated += OnAudioLevelsUpdated;
        _dictation.TranscriptReceived += OnTranscriptReceived;
        _dictation.StateChanged += OnStateChanged;

        ToggleDictationCommand = new RelayCommand(() =>
        {
            if (_dictation.IsListening)
            {
                _dictation.StopDictation();
            }
            else
            {
                _dictation.StartDictation();
            }
        });

        CancelDictationCommand = new RelayCommand(_dictation.CancelDictation);
    }

    public bool IsListening => _dictation.IsListening;

    public Microsoft.UI.Xaml.Visibility PillVisibility =>
        _dictation.IsListening ? Microsoft.UI.Xaml.Visibility.Visible : Microsoft.UI.Xaml.Visibility.Collapsed;

    public float[] AudioLevels
    {
        get => _audioLevels;
        private set
        {
            _audioLevels = value;
            OnPropertyChanged();
        }
    }

    public string LastTranscript
    {
        get => _lastTranscript;
        private set
        {
            if (_lastTranscript != value)
            {
                _lastTranscript = value;
                OnPropertyChanged();
            }
        }
    }

    public ICommand ToggleDictationCommand { get; }
    public ICommand CancelDictationCommand { get; }

    private void OnAudioLevelsUpdated(object? sender, float[] levels)
    {
        if (levels != null && levels.Length == 9)
        {
            AudioLevels = levels;
        }
    }

    private void OnTranscriptReceived(object? sender, SpeechTranscriptEventArgs e)
    {
        LastTranscript = e.Text;
    }

    private void OnStateChanged(object? sender, bool isListening)
    {
        OnPropertyChanged(nameof(IsListening));
        OnPropertyChanged(nameof(PillVisibility));
        if (!isListening)
        {
            AudioLevels = new float[9];
        }
    }

    public void Dispose()
    {
        if (!_disposed)
        {
            _dictation.AudioLevelsUpdated -= OnAudioLevelsUpdated;
            _dictation.TranscriptReceived -= OnTranscriptReceived;
            _dictation.StateChanged -= OnStateChanged;
            _disposed = true;
        }
        GC.SuppressFinalize(this);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
