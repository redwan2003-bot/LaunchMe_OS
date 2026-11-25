using LaunchMe.Contracts.Triggers;
using LaunchMe.Platform.Win32.Gestures;
using Xunit;

namespace LaunchMe.Platform.Tests;

public sealed class TouchpadGestureTests
{
    [Fact]
    public void CalculateAverageDistanceToCentroid_KnownPoints_ReturnsAccurateDistance()
    {
        // 4 points on square: (-10, -10), (10, -10), (10, 10), (-10, 10)
        // Centroid is (0, 0), each distance is sqrt(100+100) = ~14.142
        var contacts = new List<TouchContact>
        {
            new(1, -10, -10),
            new(2, 10, -10),
            new(3, 10, 10),
            new(4, -10, 10)
        };

        double avgDist = TouchpadGestureParser.CalculateAverageDistanceToCentroid(contacts);
        Assert.InRange(avgDist, 14.14, 14.15);
    }

    [Fact]
    public void FeedFrame_3FingerPinch_TriggersTouchpadPinch()
    {
        var parser = new TouchpadGestureParser(thresholdFraction: 0.15, debounceMs: 100)
        {
            TargetFingerCount = 3
        };

        TriggerType? triggered = null;
        parser.GestureTriggered += (_, e) => triggered = e.TriggerType;

        var now = DateTime.UtcNow;

        // Frame 1: Initial spread
        var frame1 = new List<TouchContact>
        {
            new(1, 100, 100),
            new(2, 200, 100),
            new(3, 150, 200)
        };
        var r1 = parser.FeedFrame(frame1, now);
        Assert.Null(r1);

        // Frame 2: Pinched inwards (distance reduced significantly)
        var frame2 = new List<TouchContact>
        {
            new(1, 130, 120),
            new(2, 170, 120),
            new(3, 150, 160)
        };
        var r2 = parser.FeedFrame(frame2, now.AddMilliseconds(20));

        Assert.Equal(TriggerType.TouchpadPinch, r2);
        Assert.Equal(TriggerType.TouchpadPinch, triggered);
    }

    [Fact]
    public void FeedFrame_4FingerSpread_TriggersTouchpadSpread()
    {
        var parser = new TouchpadGestureParser(thresholdFraction: 0.15, debounceMs: 100)
        {
            TargetFingerCount = 4
        };

        TriggerType? triggered = null;
        parser.GestureTriggered += (_, e) => triggered = e.TriggerType;

        var now = DateTime.UtcNow;

        // Frame 1: Concentrated fingers
        var frame1 = new List<TouchContact>
        {
            new(1, 140, 140),
            new(2, 160, 140),
            new(3, 160, 160),
            new(4, 140, 160)
        };
        parser.FeedFrame(frame1, now);

        // Frame 2: Spread outwards
        var frame2 = new List<TouchContact>
        {
            new(1, 100, 100),
            new(2, 200, 100),
            new(3, 200, 200),
            new(4, 100, 200)
        };
        var r2 = parser.FeedFrame(frame2, now.AddMilliseconds(20));

        Assert.Equal(TriggerType.TouchpadSpread, r2);
        Assert.Equal(TriggerType.TouchpadSpread, triggered);
    }

    [Fact]
    public void FeedFrame_MismatchedFingerCount_Ignored()
    {
        var parser = new TouchpadGestureParser { TargetFingerCount = 3 };

        // Only 2 fingers provided
        var frame = new List<TouchContact>
        {
            new(1, 100, 100),
            new(2, 200, 100)
        };

        var result = parser.FeedFrame(frame, DateTime.UtcNow);
        Assert.Null(result);
    }

    [Fact]
    public void FeedFrame_Debounce_PreventsRapidDuplicateTriggers()
    {
        var parser = new TouchpadGestureParser(thresholdFraction: 0.15, debounceMs: 300)
        {
            TargetFingerCount = 3
        };

        int triggerCount = 0;
        parser.GestureTriggered += (_, _) => triggerCount++;

        var now = DateTime.UtcNow;
        var fInitial = new List<TouchContact> { new(1, 100, 100), new(2, 200, 100), new(3, 150, 200) };
        var fPinch = new List<TouchContact> { new(1, 130, 120), new(2, 170, 120), new(3, 150, 160) };

        parser.FeedFrame(fInitial, now);
        parser.FeedFrame(fPinch, now.AddMilliseconds(20));
        Assert.Equal(1, triggerCount);

        // Immediate next pinch before debounce interval expires
        parser.FeedFrame(fInitial, now.AddMilliseconds(50));
        parser.FeedFrame(fPinch, now.AddMilliseconds(70));
        Assert.Equal(1, triggerCount); // Debounced

        // After debounce interval expires
        parser.FeedFrame(fInitial, now.AddMilliseconds(350));
        parser.FeedFrame(fPinch, now.AddMilliseconds(370));
        Assert.Equal(2, triggerCount); // Allowed
    }
}
