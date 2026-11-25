using FluentAssertions;
using LaunchMe.Core.Voice;
using LaunchMe.UI.Voice;
using Xunit;

namespace LaunchMe.UI.Tests.Voice;

public class FlowPillViewModelTests
{
    [Fact]
    public void InitialState_IsHiddenWithZeroLevels()
    {
        var service = new FlowDictationService();
        var vm = new FlowPillViewModel(service);

        vm.IsListening.Should().BeFalse();
        vm.PillVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
        vm.AudioLevels.Should().HaveCount(9);
    }

    [Fact]
    public void ToggleDictationCommand_StartsAndStopsListening()
    {
        var service = new FlowDictationService();
        var vm = new FlowPillViewModel(service);

        vm.ToggleDictationCommand.Execute(null);
        vm.IsListening.Should().BeTrue();
        vm.PillVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Visible);

        vm.ToggleDictationCommand.Execute(null);
        vm.IsListening.Should().BeFalse();
        vm.PillVisibility.Should().Be(Microsoft.UI.Xaml.Visibility.Collapsed);
    }

    [Fact]
    public void AudioLevelsFeed_UpdatesLevelsArray()
    {
        var service = new FlowDictationService();
        var vm = new FlowPillViewModel(service);
        service.StartDictation();

        var testLevels = new float[] { 0.1f, 0.2f, 0.3f, 0.4f, 0.5f, 0.6f, 0.7f, 0.8f, 0.9f };
        service.FeedAudioLevels(testLevels);

        vm.AudioLevels.Should().Equal(testLevels);
    }
}
