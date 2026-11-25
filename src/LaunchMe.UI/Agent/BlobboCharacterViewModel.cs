using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using LaunchMe.Contracts.Agent;
using LaunchMe.UI.ViewModels;

namespace LaunchMe.UI.Agent;

/// <summary>
/// ViewModel coordinating Blobbo Snoot character animations, mood states, and eye-tracking cursor responses.
/// </summary>
public class BlobboCharacterViewModel : INotifyPropertyChanged
{
    private readonly IBlobboController _blobbo;

    public BlobboCharacterViewModel(IBlobboController blobbo)
    {
        _blobbo = blobbo;
        _blobbo.MoodChanged += (_, _) =>
        {
            OnPropertyChanged(nameof(Mood));
            OnPropertyChanged(nameof(MoodEmoji));
            OnPropertyChanged(nameof(MoodDescription));
        };

        PokeCommand = new RelayCommand(_blobbo.Poke);
    }

    public BlobboMood Mood => _blobbo.CurrentMood;

    public (double X, double Y) PupilOffset => _blobbo.PupilOffset;

    public string MoodEmoji => _blobbo.CurrentMood switch
    {
        BlobboMood.Idle => "( •̀ ω •́ )",
        BlobboMood.Listening => "( ⊙ _ ⊙ )",
        BlobboMood.Thinking => "( ¬ _ ¬ )",
        BlobboMood.Poked => "( > _ < )",
        BlobboMood.Dizzy => "( @ _ @ )",
        BlobboMood.Sleeping => "( - _ - ) zzz",
        BlobboMood.Dancing => @"\( ^ヮ^ )/",
        BlobboMood.Alert => "( ◕ ‿ ◕ )!",
        _ => "( •̀ ω •́ )"
    };

    public string MoodDescription => _blobbo.CurrentMood switch
    {
        BlobboMood.Idle => "Blobbo is watching quietly",
        BlobboMood.Listening => "Blobbo is listening to you",
        BlobboMood.Thinking => "Pookie is busy working",
        BlobboMood.Poked => "Hey! That tickles!",
        BlobboMood.Dizzy => "Woah, too many pokes!",
        BlobboMood.Sleeping => "Shh... Blobbo is napping",
        BlobboMood.Dancing => "Task completed! Yay!",
        BlobboMood.Alert => "Pookie needs your permission!",
        _ => "Blobbo is here"
    };

    public ICommand PokeCommand { get; }

    public void UpdatePupilPhysics(double cursorX, double cursorY, double snootCenterX, double snootCenterY)
    {
        _blobbo.UpdatePupils(cursorX, cursorY, snootCenterX, snootCenterY);
        OnPropertyChanged(nameof(PupilOffset));
    }

    public void SetMood(BlobboMood mood) => _blobbo.SetMood(mood);

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
