using LaunchMe.Contracts.Media;
using LaunchMe.UI.ViewModels;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class MusicPlayerViewModelTests : IDisposable
{
    private sealed class MockMediaSessionService : IMediaSessionService
    {
        public bool PlayPauseCalled { get; private set; }
        public bool NextCalled { get; private set; }
        public bool PrevCalled { get; private set; }
        public TimeSpan? SeekPosition { get; private set; }

        public event EventHandler<MediaTrackInfo>? TrackChanged;

        public Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default)
        {
            return Task.FromResult<MediaTrackInfo?>(new MediaTrackInfo
            {
                Title = "Starlight",
                Artist = "Muse",
                AlbumTitle = "Black Holes and Revelations",
                Duration = TimeSpan.FromMinutes(4),
                Position = TimeSpan.FromSeconds(30),
                IsPlaying = true
            });
        }

        public Task PlayPauseAsync(CancellationToken ct = default)
        {
            PlayPauseCalled = true;
            return Task.CompletedTask;
        }

        public Task NextAsync(CancellationToken ct = default)
        {
            NextCalled = true;
            return Task.CompletedTask;
        }

        public Task PreviousAsync(CancellationToken ct = default)
        {
            PrevCalled = true;
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken ct = default)
        {
            SeekPosition = position;
            return Task.CompletedTask;
        }

        public void RaiseTrack(MediaTrackInfo track) => TrackChanged?.Invoke(this, track);
        public void Dispose() { }
    }

    private readonly MockMediaSessionService _mockMedia = new();
    private readonly MusicPlayerViewModel _vm;

    public MusicPlayerViewModelTests()
    {
        _vm = new MusicPlayerViewModel(_mockMedia);
    }

    public void Dispose()
    {
        _vm.Dispose();
        _mockMedia.Dispose();
    }

    [Fact]
    public void CycleVisualizerMode_CyclesAllFourModes()
    {
        Assert.Equal(MusicVisualizerMode.Vinyl, _vm.VisualizerMode);

        _vm.CycleVisualizerMode();
        Assert.Equal(MusicVisualizerMode.FullScreenArt, _vm.VisualizerMode);

        _vm.CycleVisualizerMode();
        Assert.Equal(MusicVisualizerMode.Particles, _vm.VisualizerMode);

        _vm.CycleVisualizerMode();
        Assert.Equal(MusicVisualizerMode.GlowingBeats, _vm.VisualizerMode);

        _vm.CycleVisualizerMode();
        Assert.Equal(MusicVisualizerMode.Vinyl, _vm.VisualizerMode);
    }

    [Fact]
    public void UpdateFromTrack_SetsPropertiesCorrectly()
    {
        var track = new MediaTrackInfo
        {
            Title = "Bohemian Rhapsody",
            Artist = "Queen",
            AlbumTitle = "A Night at the Opera",
            Duration = TimeSpan.FromMinutes(6),
            Position = TimeSpan.FromMinutes(2),
            IsPlaying = true
        };

        _vm.UpdateFromTrack(track);

        Assert.Equal("Bohemian Rhapsody", _vm.TrackTitle);
        Assert.Equal("Queen", _vm.Artist);
        Assert.Equal("A Night at the Opera", _vm.Album);
        Assert.True(_vm.IsPlaying);
        Assert.Equal("02:00", _vm.PositionFormatted);
        Assert.Equal("06:00", _vm.DurationFormatted);
        Assert.InRange(_vm.ProgressFraction, 0.33, 0.34);
    }

    [Fact]
    public async Task TransportCommands_DelegateToMediaService()
    {
        await _vm.PlayPauseAsync();
        Assert.True(_mockMedia.PlayPauseCalled);

        await _vm.NextAsync();
        Assert.True(_mockMedia.NextCalled);

        await _vm.PreviousAsync();
        Assert.True(_mockMedia.PrevCalled);

        _vm.Duration = TimeSpan.FromSeconds(200);
        await _vm.SeekAsync(0.5);
        Assert.Equal(TimeSpan.FromSeconds(100), _mockMedia.SeekPosition);
    }
}
