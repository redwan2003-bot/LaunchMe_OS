namespace LaunchMe.Contracts.Media;

/// <summary>
/// Bridge to Windows GlobalSystemMediaTransportControlsSessionManager (GSMTC).
/// </summary>
public interface IMediaSessionService : IDisposable
{
    Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default);
    Task PlayPauseAsync(CancellationToken ct = default);
    Task NextAsync(CancellationToken ct = default);
    Task PreviousAsync(CancellationToken ct = default);
    Task SeekAsync(TimeSpan position, CancellationToken ct = default);

    event EventHandler<MediaTrackInfo>? TrackChanged;
}
