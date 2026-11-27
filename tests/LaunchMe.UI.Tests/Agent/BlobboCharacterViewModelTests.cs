using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using LaunchMe.UI.Agent;
using Xunit;

namespace LaunchMe.UI.Tests.Agent;

public class BlobboCharacterViewModelTests
{
    [Fact]
    public void InitialState_IsIdleWithIdleEmoji()
    {
        var blobbo = new BlobboController();
        var vm = new BlobboCharacterViewModel(blobbo);

        vm.Mood.Should().Be(BlobboMood.Idle);
        vm.MoodEmoji.Should().Be("( •̀ ω •́ )");
        vm.MoodDescription.Should().Contain("Blobbo is watching quietly");
    }

    [Fact]
    public void SetMood_UpdatesEmojiAndDescription()
    {
        var blobbo = new BlobboController();
        var vm = new BlobboCharacterViewModel(blobbo);

        vm.SetMood(BlobboMood.Thinking);

        vm.Mood.Should().Be(BlobboMood.Thinking);
        vm.MoodEmoji.Should().Be("( ¬ _ ¬ )");
        vm.MoodDescription.Should().Contain("Blobbo is busy working");
    }

    [Fact]
    public void PokeCommand_TriggersPokedMood()
    {
        var blobbo = new BlobboController();
        var vm = new BlobboCharacterViewModel(blobbo);

        vm.PokeCommand.Execute(null);

        vm.Mood.Should().Be(BlobboMood.Poked);
        vm.MoodEmoji.Should().Be("( > _ < )");
    }
}
