using LaunchMe.Contracts.Agent;

namespace LaunchMe.Core.Agent;

/// <summary>
/// State machine and physics coordinator for the Blobbo Snoot HUD pet.
/// </summary>
public class BlobboController : IBlobboController
{
    private BlobboMood _currentMood = BlobboMood.Idle;
    private (double PupilX, double PupilY) _pupilOffset = (0, 0);
    private int _consecutivePokes;
    private DateTime _lastPokeTime = DateTime.MinValue;

    public BlobboMood CurrentMood => _currentMood;
    public (double PupilX, double PupilY) PupilOffset => _pupilOffset;

    public event EventHandler<BlobboMood>? MoodChanged;

    public void SetMood(BlobboMood mood)
    {
        if (_currentMood != mood)
        {
            _currentMood = mood;
            MoodChanged?.Invoke(this, mood);
        }
    }

    public void UpdatePupils(double cursorX, double cursorY, double snootCenterX, double snootCenterY)
    {
        if (_currentMood == BlobboMood.Sleeping || _currentMood == BlobboMood.Dizzy)
        {
            _pupilOffset = (0, 0);
            return;
        }

        double dx = cursorX - snootCenterX;
        double dy = cursorY - snootCenterY;
        double distance = Math.Sqrt((dx * dx) + (dy * dy));

        if (distance < 1.0)
        {
            _pupilOffset = (0, 0);
            return;
        }

        const double maxRadius = 5.0; // Max pupil excursion in pixels
        double factor = Math.Min(1.0, distance / 400.0); // Reach max at 400px distance
        double targetX = (dx / distance) * maxRadius * factor;
        double targetY = (dy / distance) * maxRadius * factor;

        _pupilOffset = (Math.Round(targetX, 2), Math.Round(targetY, 2));
    }

    public void Poke()
    {
        var now = DateTime.UtcNow;
        if ((now - _lastPokeTime).TotalSeconds < 2.0)
        {
            _consecutivePokes++;
        }
        else
        {
            _consecutivePokes = 1;
        }

        _lastPokeTime = now;

        if (_consecutivePokes >= 3)
        {
            SetMood(BlobboMood.Dizzy);
        }
        else
        {
            SetMood(BlobboMood.Poked);
        }
    }
}
