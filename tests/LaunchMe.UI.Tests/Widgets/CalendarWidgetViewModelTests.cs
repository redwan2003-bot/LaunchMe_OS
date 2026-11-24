using FluentAssertions;
using LaunchMe.UI.Widgets.Calendar;
using Xunit;

namespace LaunchMe.UI.Tests.Widgets;

public sealed class CalendarWidgetViewModelTests
{
    [Fact]
    public void PopulateCalendarDays_GeneratesExactly42Days()
    {
        // Arrange & Act
        using var vm = new CalendarWidgetViewModel(new DateTime(2026, 11, 24));

        // Assert: 6 rows * 7 columns
        vm.Days.Should().HaveCount(42);
        vm.MonthYearTitle.Should().Be("November 2026");
    }

    [Fact]
    public void NextAndPreviousMonthCommands_UpdateViewMonthAndDays()
    {
        // Arrange
        using var vm = new CalendarWidgetViewModel(new DateTime(2026, 11, 1));

        // Act 1: Next month
        vm.NextMonthCommand.Execute(null);

        // Assert 1: December 2026
        vm.MonthYearTitle.Should().Be("December 2026");
        vm.Days.Should().HaveCount(42);

        // Act 2: Previous month twice
        vm.PreviousMonthCommand.Execute(null);
        vm.PreviousMonthCommand.Execute(null);

        // Assert 2: October 2026
        vm.MonthYearTitle.Should().Be("October 2026");
    }

    [Fact]
    public void DaySelection_UpdatesSelectedDateAndFlags()
    {
        // Arrange
        using var vm = new CalendarWidgetViewModel(new DateTime(2026, 11, 10));

        // Act: Select 15th
        var targetDay = vm.Days.First(d => d.IsCurrentMonth && d.DayNumber == 15);
        vm.SelectedDate = targetDay.Date;

        // Assert
        targetDay.IsSelected.Should().BeTrue();
        vm.SelectedDate.Day.Should().Be(15);
    }
}
