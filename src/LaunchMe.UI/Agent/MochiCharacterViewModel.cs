using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Agent;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Agent;

/// <summary>
/// ViewModel coordinating Mochi character animations, mood states, and eye-tracking cursor responses.
/// </summary>
public class MochiCharacterViewModel : INotifyPropertyChanged
{
    private readonly IMochiController _mochi;

    public MochiCharacterViewModel(IMochiController mochi)
    {
        _mochi = mochi;
        _mochi.MoodChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Mood));
            OnPropertyChanged(nameof(MoodEmoji));
            OnPropertyChanged(nameof(MoodDescription));
        };

        PokeCommand = new RelayCommand(_mochi.Poke);
    }

    public MochiMood Mood => _mochi.CurrentMood;

    public (double X, double Y) PupilOffset => _mochi.PupilOffset;

    public string MoodEmoji => _mochi.CurrentMood switch
    {
        MochiMood.Idle => "( •̀ ω •́ )",
        MochiMood.Listening => "( ⊙ _ ⊙ )",
        MochiMood.Thinking => "( ¬ _ ¬ )",
        MochiMood.Poked => "( > _ < )",
        MochiMood.Dizzy => "( @ _ @ )",
        MochiMood.Sleeping => "( - _ - ) zzz",
        MochiMood.Dancing => @"\( ^ヮ^ )/",
        MochiMood.Alert => "( ◕ ‿ ◕ )!",
        _ => "( •̀ ω •́ )"
    };

    public string MoodDescription => _mochi.CurrentMood switch
    {
        MochiMood.Idle => "Mochi is watching quietly",
        MochiMood.Listening => "Mochi is listening to you",
        MochiMood.Thinking => "Mochi's agent is busy working",
        MochiMood.Poked => "Hey! That tickles!",
        MochiMood.Dizzy => "Woah, too many pokes!",
        MochiMood.Sleeping => "Shh... Mochi is resting",
        MochiMood.Dancing => "Task completed! Yay!",
        MochiMood.Alert => "Agent needs your permission!",
        _ => "Mochi is here"
    };

    public ICommand PokeCommand { get; }

    public void UpdatePupilPhysics(double cursorX, double cursorY, double notchCenterX, double notchCenterY)
    {
        _mochi.UpdatePupils(cursorX, cursorY, notchCenterX, notchCenterY);
        OnPropertyChanged(nameof(PupilOffset));
    }

    public void SetMood(MochiMood mood) => _mochi.SetMood(mood);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
