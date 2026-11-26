using System.Xml.Linq;
using FluentAssertions;
using LaunchMe.UI.Services;
using Xunit;

namespace LaunchMe.UI.Tests;

public sealed class LocalizationServiceTests
{
    private static string FindStringsDirectory()
    {
        string dir = AppContext.BaseDirectory;
        for (int i = 0; i < 8; i++)
        {
            string candidate = Path.Combine(dir, "src", "LaunchMe.UI", "Strings");
            if (Directory.Exists(candidate)) return candidate;
            dir = Path.GetDirectoryName(dir)!;
        }
        throw new DirectoryNotFoundException("Could not locate src/LaunchMe.UI/Strings");
    }

    [Fact]
    public void All18Cultures_ExistHaveFullKeyParityAndAreTranslated()
    {
        string stringsDir = FindStringsDirectory();
        LocalizationService.SupportedCultures.Should().HaveCount(18);

        var enDoc = XDocument.Load(Path.Combine(stringsDir, "en-US", "Resources.resw"));
        var enMap = enDoc.Descendants("data")
            .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element("value")!.Value);

        enMap.Count.Should().BeGreaterThanOrEqualTo(40);

        foreach (string culture in LocalizationService.SupportedCultures)
        {
            string reswPath = Path.Combine(stringsDir, culture, "Resources.resw");
            File.Exists(reswPath).Should().BeTrue($"missing {culture}/Resources.resw");

            var doc = XDocument.Load(reswPath);
            var map = doc.Descendants("data")
                .ToDictionary(d => d.Attribute("name")!.Value, d => d.Element("value")!.Value);

            map.Keys.Should().BeEquivalentTo(enMap.Keys, $"{culture} must match en-US keys 100%");

            if (culture != "en-US")
            {
                int translatedCount = map.Count(kv => kv.Value != enMap[kv.Key]);
                translatedCount.Should().BeGreaterThan(25, $"{culture} must not be English placeholder text");
            }
        }
    }

    [Fact]
    public void LocalizationService_ResolvesArabicRtlAndStrings()
    {
        var ar = new LocalizationService("ar-SA");
        ar.IsRightToLeft.Should().BeTrue();
        ar.GetString("SettingsTitle").Should().Be("الإعدادات");

        var ja = new LocalizationService("ja-JP");
        ja.IsRightToLeft.Should().BeFalse();
        ja.GetString("NotchClipboard").Should().Be("クリップボード");
    }
}
