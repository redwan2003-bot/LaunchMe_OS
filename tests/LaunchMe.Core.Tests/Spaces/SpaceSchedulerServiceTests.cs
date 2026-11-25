using FluentAssertions;
using LaunchMe.Contracts.Spaces;
using LaunchMe.Core.Spaces;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests.Spaces;

public sealed class SpaceSchedulerServiceTests
{
    private static SpaceModel CreateSpace(
        string id,
        string name,
        bool isDefault,
        List<string> days,
        string start,
        string end)
    {
        return new SpaceModel
        {
            SpaceId = id,
            Name = name,
            IsDefault = isDefault,
            Schedule = new SpaceSchedule
            {
                Days = days,
                Start = start,
                End = end
            }
        };
    }

    [Theory]
    [InlineData("Mon", "09:00", "18:00", 2026, 11, 23, 9, 0, true)]
    [InlineData("monday", "9:00", "18:00", 2026, 11, 23, 12, 30, true)]
    [InlineData("MON", "09:00", "18:00", 2026, 11, 23, 18, 0, false)]
    [InlineData("Tue", "09:00", "18:00", 2026, 11, 23, 10, 0, false)]
    [InlineData("Wednesday", "8:30", "17:45", 2026, 11, 25, 8, 30, true)]
    public async Task EvaluateScheduleAsync_WeekdayAndTimeMatching_RespectsFormatsAndBounds(
        string dayEntry,
        string start,
        string end,
        int year,
        int month,
        int day,
        int hour,
        int minute,
        bool shouldMatch)
    {
        var defaultSpace = CreateSpace("default", "Default", isDefault: true, [], "00:00", "00:00");
        var workSpace = CreateSpace("work", "Work", isDefault: false, [dayEntry], start, end);

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([defaultSpace, workSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSpace);
        store.Setup(s => s.SwitchSpaceAsync("work", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        using var scheduler = new SpaceSchedulerService(store.Object);
        SpaceChangedEventArgs? raisedArgs = null;
        scheduler.ScheduledSpaceActivated += (_, args) => raisedArgs = args;

        var checkTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);
        var result = await scheduler.EvaluateScheduleAsync(checkTime);

        if (shouldMatch)
        {
            result.Should().NotBeNull();
            result!.SpaceId.Should().Be("work");
            store.Verify(s => s.SwitchSpaceAsync("work", It.IsAny<CancellationToken>()), Times.Once);
            raisedArgs.Should().NotBeNull();
            raisedArgs!.PreviousSpaceId.Should().Be("default");
            raisedArgs.NewSpaceId.Should().Be("work");
        }
        else
        {
            result.Should().BeNull();
            store.Verify(s => s.SwitchSpaceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
            raisedArgs.Should().BeNull();
        }
    }

    [Theory]
    [InlineData(2026, 11, 27, 22, 0, true)]   // Friday 22:00 -> start of window
    [InlineData(2026, 11, 27, 23, 45, true)]  // Friday 23:45 -> pre-midnight
    [InlineData(2026, 11, 28, 2, 30, true)]   // Saturday 02:30 -> post-midnight of Friday's window
    [InlineData(2026, 11, 28, 6, 0, false)]   // Saturday 06:00 -> end of window
    [InlineData(2026, 11, 27, 2, 30, false)]  // Friday 02:30 -> belongs to Thursday, not Friday
    [InlineData(2026, 11, 27, 15, 0, false)]  // Friday 15:00 -> outside window
    public async Task EvaluateScheduleAsync_OvernightSchedule_MatchesAcrossMidnight(
        int year,
        int month,
        int day,
        int hour,
        int minute,
        bool shouldMatch)
    {
        var defaultSpace = CreateSpace("default", "Default", isDefault: true, [], "00:00", "00:00");
        var nightSpace = CreateSpace("night", "Night Focus", isDefault: false, ["Fri"], "22:00", "06:00");

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([defaultSpace, nightSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSpace);

        using var scheduler = new SpaceSchedulerService(store.Object);
        var checkTime = new DateTimeOffset(year, month, day, hour, minute, 0, TimeSpan.Zero);

        var result = await scheduler.EvaluateScheduleAsync(checkTime);

        if (shouldMatch)
        {
            result.Should().NotBeNull();
            result!.SpaceId.Should().Be("night");
            store.Verify(s => s.SwitchSpaceAsync("night", It.IsAny<CancellationToken>()), Times.Once);
        }
        else
        {
            result.Should().BeNull();
            store.Verify(s => s.SwitchSpaceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }

    [Fact]
    public async Task EvaluateScheduleAsync_WhenMatchedSpaceIsAlreadyActive_SkipsSwitching()
    {
        var workSpace = CreateSpace("work", "Work", isDefault: false, ["Mon"], "09:00", "18:00");

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([workSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(workSpace);

        using var scheduler = new SpaceSchedulerService(store.Object);
        var eventCount = 0;
        scheduler.ScheduledSpaceActivated += (_, _) => eventCount++;

        var mondayMorning = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);
        var result = await scheduler.EvaluateScheduleAsync(mondayMorning);

        result.Should().BeNull();
        eventCount.Should().Be(0);
        store.Verify(s => s.SwitchSpaceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task EvaluateScheduleAsync_PrioritizesNonDefaultOverDefaultSpace()
    {
        var defaultSpace = CreateSpace("default", "Default", isDefault: true, ["Mon"], "09:00", "18:00");
        var workSpace = CreateSpace("work", "Work", isDefault: false, ["Mon"], "09:00", "18:00");

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([defaultSpace, workSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(defaultSpace);

        using var scheduler = new SpaceSchedulerService(store.Object);
        var mondayMorning = new DateTimeOffset(2026, 11, 23, 11, 0, 0, TimeSpan.Zero);

        var result = await scheduler.EvaluateScheduleAsync(mondayMorning);

        result.Should().NotBeNull();
        result!.SpaceId.Should().Be("work");
        store.Verify(s => s.SwitchSpaceAsync("work", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EvaluateScheduleAsync_WhenDefaultHasNoMatchingWindow_SkipsSwitching()
    {
        var defaultSpace = CreateSpace("default", "Default", isDefault: true, ["Mon"], "09:00", "18:00");
        var otherSpace = CreateSpace("other", "Other", isDefault: false, ["Tue"], "09:00", "18:00");

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([defaultSpace, otherSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(otherSpace);

        using var scheduler = new SpaceSchedulerService(store.Object);
        var sunday = new DateTimeOffset(2026, 11, 22, 12, 0, 0, TimeSpan.Zero);

        var result = await scheduler.EvaluateScheduleAsync(sunday);

        result.Should().BeNull();
        store.Verify(s => s.SwitchSpaceAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task TimerLifecycle_StartStopAndDispose_EvaluatesAndStopsCleanly()
    {
        var defaultSpace = CreateSpace("default", "Default", isDefault: true, [], "00:00", "00:00");
        var workSpace = CreateSpace("work", "Work", isDefault: false, ["Mon"], "09:00", "18:00");
        var mondayMorning = new DateTimeOffset(2026, 11, 23, 10, 0, 0, TimeSpan.Zero);

        var activatedTcs = new TaskCompletionSource<SpaceChangedEventArgs>(TaskCreationOptions.RunContinuationsAsynchronously);
        var activeSpace = defaultSpace;

        var store = new Mock<ISpaceStore>();
        store.Setup(s => s.GetAllSpacesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([defaultSpace, workSpace]);
        store.Setup(s => s.GetActiveSpaceAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => activeSpace);
        store.Setup(s => s.SwitchSpaceAsync("work", It.IsAny<CancellationToken>()))
            .Callback(() => activeSpace = workSpace)
            .Returns(Task.CompletedTask);

        var scheduler = new SpaceSchedulerService(
            store.Object,
            pollInterval: TimeSpan.FromMilliseconds(25),
            clock: () => mondayMorning);

        scheduler.ScheduledSpaceActivated += (_, args) => activatedTcs.TrySetResult(args);

        scheduler.IsRunning.Should().BeFalse();
        scheduler.Start();
        scheduler.IsRunning.Should().BeTrue();

        var completed = await Task.WhenAny(activatedTcs.Task, Task.Delay(TimeSpan.FromSeconds(3)));
        completed.Should().Be(activatedTcs.Task);
        (await activatedTcs.Task).NewSpaceId.Should().Be("work");

        scheduler.Stop();
        scheduler.IsRunning.Should().BeFalse();

        // Idempotent restart and stop
        scheduler.Start();
        scheduler.IsRunning.Should().BeTrue();
        scheduler.Stop();
        scheduler.IsRunning.Should().BeFalse();

        // Deterministic disposal
        scheduler.Dispose();
        scheduler.Dispose();
        scheduler.IsRunning.Should().BeFalse();

        Action startAfterDispose = () => scheduler.Start();
        startAfterDispose.Should().Throw<ObjectDisposedException>();

        Func<Task> evalAfterDispose = () => scheduler.EvaluateScheduleAsync(mondayMorning);
        await evalAfterDispose.Should().ThrowAsync<ObjectDisposedException>();
    }
}
