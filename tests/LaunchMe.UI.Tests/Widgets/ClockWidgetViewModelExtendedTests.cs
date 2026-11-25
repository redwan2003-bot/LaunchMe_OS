using FluentAssertions;
using LaunchMe.UI.Widgets.Clock;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

/// <summary>
/// Extended ClockWidgetViewModel tests covering all 12 luxury skins,
/// flip-flap digit chars, and UTC dual-time output.
/// </summary>
public sealed class ClockWidgetViewModelExtendedTests
{
    // ── All 12 ClockStyle values should be selectable ──────────────────────
    [Theory]
    [InlineData(ClockStyle.DigitalMinimal)]
    [InlineData(ClockStyle.DigitalBold)]
    [InlineData(ClockStyle.DigitalSquared)]
    [InlineData(ClockStyle.AnalogClassic)]
    [InlineData(ClockStyle.AnalogMinimal)]
    [InlineData(ClockStyle.AnalogChrono)]
    [InlineData(ClockStyle.FlipSplitFlap)]
    [InlineData(ClockStyle.SwissRail)]
    [InlineData(ClockStyle.EditorialSerif)]
    [InlineData(ClockStyle.ConcentricRing)]
    [InlineData(ClockStyle.NeonBinary)]
    [InlineData(ClockStyle.WorldDualTime)]
    public void Style_CanBeSetToAllTwelveSkins(ClockStyle style)
    {
        using var vm = new ClockWidgetViewModel(startActive: false);
        vm.Style = style;
        vm.Style.Should().Be(style);
    }

    // ── Default skin is DigitalMinimal ──────────────────────────────────────
    [Fact]
    public void DefaultStyle_IsDigitalMinimal()
    {
        using var vm = new ClockWidgetViewModel(startActive: false);
        vm.Style.Should().Be(ClockStyle.DigitalMinimal);
    }

    // ── Flip-flap digit decomposition ───────────────────────────────────────
    [Fact]
    public void FlipDigits_MatchTimeComponents_24HourMode()
    {
        // 14:30 → H1="1", H2="4", M1="3", M2="0"
        using var vm = new ClockWidgetViewModel(startActive: false);
        vm.Is24HourFormat = true;
        vm.SetTime(new DateTime(2026, 11, 24, 14, 30, 0));

        vm.FlipH1.Should().Be("1");
        vm.FlipH2.Should().Be("4");
        vm.FlipM1.Should().Be("3");
        vm.FlipM2.Should().Be("0");
    }

    [Fact]
    public void FlipDigits_MatchTimeComponents_12HourMode()
    {
        // 02:05 PM → H1="0", H2="2", M1="0", M2="5"
        using var vm = new ClockWidgetViewModel(startActive: false);
        vm.Is24HourFormat = false;
        vm.SetTime(new DateTime(2026, 11, 24, 14, 5, 0));

        vm.FlipH1.Should().Be("0");
        vm.FlipH2.Should().Be("2");
        vm.FlipM1.Should().Be("0");
        vm.FlipM2.Should().Be("5");
    }

    // ── UTC dual-time readout ────────────────────────────────────────────────
    [Fact]
    public void UtcTime_ReflectsUtcOffset_FromLocalTime()
    {
        using var vm = new ClockWidgetViewModel(startActive: false);
        vm.Is24HourFormat = true;
        vm.ShowSeconds = false;

        // Use DateTimeKind.Local so ToUniversalTime() applies the local offset
        var localTime = new DateTime(2026, 11, 24, 12, 0, 0, DateTimeKind.Local);
        vm.SetTime(localTime);

        // The UTC string should not equal local if there's any offset
        string expectedUtc = localTime.ToUniversalTime().ToString("HH:mm");
        vm.UtcTime.Should().Be(expectedUtc);
    }

    // ── PropertyChanged fires on Style change ───────────────────────────────
    [Fact]
    public void Style_PropertyChanged_FiresOnChange()
    {
        using var vm = new ClockWidgetViewModel(startActive: false);
        string? changedProp = null;
        vm.PropertyChanged += (_, e) => changedProp = e.PropertyName;

        vm.Style = ClockStyle.NeonBinary;

        changedProp.Should().Be(nameof(ClockWidgetViewModel.Style));
    }

    // ── Enum has exactly 12 members ─────────────────────────────────────────
    [Fact]
    public void ClockStyleEnum_HasExactlyTwelveMembers()
    {
        var values = Enum.GetValues<ClockStyle>();
        values.Should().HaveCount(12, "LaunchMe ships 12 luxury clock skins");
    }
}
