using FluentAssertions;
using LaunchMe.Contracts.Agent;
using LaunchMe.Core.Agent;
using Xunit;

namespace LaunchMe.Core.Tests.Agent;

public class DiffParserServiceTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmptyDocument()
    {
        var parser = new DiffParserService();
        var doc = parser.Parse(string.Empty);

        doc.Files.Should().BeEmpty();
    }

    [Fact]
    public void Parse_UnifiedDiff_ParsesAddedDeletedAndContextLines()
    {
        var parser = new DiffParserService();
        string diff = @"--- a/file.txt
+++ b/file.txt
@@ -1,3 +1,3 @@
 context line
-old line
+new line";

        var doc = parser.Parse(diff);

        doc.Files.Should().HaveCount(1);
        var file = doc.Files[0];
        file.OldPath.Should().Be("a/file.txt");
        file.NewPath.Should().Be("b/file.txt");

        file.Lines.Should().HaveCount(4); // 1 header, 1 context, 1 deleted, 1 added
        file.Lines[0].Type.Should().Be(DiffLineType.Header);
        file.Lines[1].Type.Should().Be(DiffLineType.Context);
        file.Lines[1].Text.Should().Be("context line");
        file.Lines[2].Type.Should().Be(DiffLineType.Deleted);
        file.Lines[2].Text.Should().Be("old line");
        file.Lines[3].Type.Should().Be(DiffLineType.Added);
        file.Lines[3].Text.Should().Be("new line");
    }
}
