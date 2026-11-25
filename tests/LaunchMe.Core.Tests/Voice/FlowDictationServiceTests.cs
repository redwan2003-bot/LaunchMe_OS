using FluentAssertions;
using LaunchMe.Contracts.Voice;
using LaunchMe.Core.Voice;
using Moq;
using Xunit;

namespace LaunchMe.Core.Tests.Voice;

public class FlowDictationServiceTests
{
    [Fact]
    public void StartAndStop_UpdatesListeningState()
    {
        var service = new FlowDictationService();

        service.StartDictation();
        service.IsListening.Should().BeTrue();

        service.StopDictation();
        service.IsListening.Should().BeFalse();
    }

    [Fact]
    public void CleanupTranscript_RemovesFillersAndCapitalizes()
    {
        var service = new FlowDictationService();

        string result = service.CleanupTranscript("um hello world uh this is great");

        result.Should().Be("Hello world this is great");
    }

    [Fact]
    public void CleanupTranscript_ReplacesConfiguredVocabulary()
    {
        var service = new FlowDictationService();
        service.AddVocabularyWord("gemini pro", "Gemini Pro");

        string result = service.CleanupTranscript("testing gemini pro on dot net");

        result.Should().Be("Testing Gemini Pro on .NET");
    }

    [Fact]
    public void FeedSpeechHypothesis_WhenFinal_InvokesKeystrokeInjector()
    {
        var injectorMock = new Mock<IKeystrokeInjector>();
        var service = new FlowDictationService(injectorMock.Object);
        service.StartDictation();

        service.FeedSpeechHypothesis("hello world", isFinal: true);

        injectorMock.Verify(i => i.InjectText("Hello world"), Times.Once);
    }
}
