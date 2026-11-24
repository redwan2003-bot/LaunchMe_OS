using LaunchMe.Contracts.Media;
using LaunchMe.Platform.Win32.Media;
using Xunit;

namespace LaunchMe.Platform.Tests;

public class MediaTests
{
    private sealed class MockGsmtcAdapter : IGsmtcAdapter
    {
        public event EventHandler<MediaTrackInfo>? TrackChanged;

        public MediaTrackInfo? CurrentTrack { get; set; }
        public bool PlayPauseCalled { get; private set; }
        public bool NextCalled { get; private set; }
        public bool PreviousCalled { get; private set; }
        public TimeSpan? SeekPosition { get; private set; }
        public bool Disposed { get; private set; }

        public Task<MediaTrackInfo?> GetCurrentTrackAsync(CancellationToken ct = default)
        {
            return Task.FromResult(CurrentTrack);
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
            PreviousCalled = true;
            return Task.CompletedTask;
        }

        public Task SeekAsync(TimeSpan position, CancellationToken ct = default)
        {
            SeekPosition = position;
            return Task.CompletedTask;
        }

        public void TriggerTrackChange(MediaTrackInfo track)
        {
            TrackChanged?.Invoke(this, track);
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }

    [Fact]
    public async Task WinRTMediaSessionService_DefaultConstructor_DoesNotThrow()
    {
        using var service = new WinRTMediaSessionService();
        var track = await service.GetCurrentTrackAsync();
        // May be null if no media player running, but must not crash
        Assert.True(track == null || !string.IsNullOrEmpty(track.Title) || !string.IsNullOrEmpty(track.SourceAppId));
    }

    [Fact]
    public async Task WinRTMediaSessionService_PassesThroughMediaCommands()
    {
        var mockAdapter = new MockGsmtcAdapter
        {
            CurrentTrack = new MediaTrackInfo
            {
                Title = "Starboy",
                Artist = "The Weeknd",
                AlbumTitle = "Starboy",
                IsPlaying = true,
                Duration = TimeSpan.FromMinutes(3.5),
                Position = TimeSpan.FromMinutes(1.2),
                SourceAppId = "Spotify"
            }
        };

        using var service = new WinRTMediaSessionService(mockAdapter);

        var track = await service.GetCurrentTrackAsync();
        Assert.NotNull(track);
        Assert.Equal("Starboy", track.Title);
        Assert.Equal("The Weeknd", track.Artist);
        Assert.True(track.IsPlaying);

        await service.PlayPauseAsync();
        Assert.True(mockAdapter.PlayPauseCalled);

        await service.NextAsync();
        Assert.True(mockAdapter.NextCalled);

        await service.PreviousAsync();
        Assert.True(mockAdapter.PreviousCalled);

        var seekTarget = TimeSpan.FromSeconds(45);
        await service.SeekAsync(seekTarget);
        Assert.Equal(seekTarget, mockAdapter.SeekPosition);
    }

    [Fact]
    public void WinRTMediaSessionService_PropagatesTrackChangedEvent()
    {
        var mockAdapter = new MockGsmtcAdapter();
        using var service = new WinRTMediaSessionService(mockAdapter);

        MediaTrackInfo? receivedTrack = null;
        service.TrackChanged += (sender, track) =>
        {
            receivedTrack = track;
        };

        var newTrack = new MediaTrackInfo
        {
            Title = "Blinding Lights",
            Artist = "The Weeknd",
            AlbumTitle = "After Hours",
            IsPlaying = true,
            SourceAppId = "Spotify"
        };

        mockAdapter.TriggerTrackChange(newTrack);

        Assert.NotNull(receivedTrack);
        Assert.Equal("Blinding Lights", receivedTrack.Title);
        Assert.Equal("The Weeknd", receivedTrack.Artist);
    }

    [Fact]
    public async Task WinRTMediaSessionService_Disposed_ThrowsExceptionAndDisposesAdapter()
    {
        var mockAdapter = new MockGsmtcAdapter();
        var service = new WinRTMediaSessionService(mockAdapter);

        service.Dispose();

        Assert.True(mockAdapter.Disposed);
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.GetCurrentTrackAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.PlayPauseAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.NextAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.PreviousAsync());
        await Assert.ThrowsAsync<ObjectDisposedException>(() => service.SeekAsync(TimeSpan.Zero));
    }
}
