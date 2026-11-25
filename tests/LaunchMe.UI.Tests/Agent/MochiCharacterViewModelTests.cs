using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using LaunchMe.UI.Agent;
using Xunit;

namespace LaunchMe.UI.Tests.Agent;

public class MochiCharacterViewModelTests
{
    [Fact]
    public void InitialState_IsIdleWithIdleEmoji()
    {
        var mochi = new MochiController();
        var vm = new MochiCharacterViewModel(mochi);

        vm.Mood.Should().Be(MochiMood.Idle);
        vm.MoodEmoji.Should().Be("( •̀ ω •́ )");
        vm.MoodDescription.Should().Contain("Mochi is watching quietly");
    }

    [Fact]
    public void SetMood_UpdatesEmojiAndDescription()
    {
        var mochi = new MochiController();
        var vm = new MochiCharacterViewModel(mochi);

        vm.SetMood(MochiMood.Thinking);

        vm.Mood.Should().Be(MochiMood.Thinking);
        vm.MoodEmoji.Should().Be("( ¬ _ ¬ )");
        vm.MoodDescription.Should().Contain("Mochi's agent is busy working");
    }

    [Fact]
    public void PokeCommand_TriggersPokedMood()
    {
        var mochi = new MochiController();
        var vm = new MochiCharacterViewModel(mochi);

        vm.PokeCommand.Execute(null);

        vm.Mood.Should().Be(MochiMood.Poked);
        vm.MoodEmoji.Should().Be("( > _ < )");
    }
}
