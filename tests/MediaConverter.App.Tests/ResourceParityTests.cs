using System.Collections;
using System.Globalization;
using System.Resources;
using MediaConverter.App.Services;
using AppResources = MediaConverter.App.Resources.Resources;

namespace MediaConverter.App.Tests;

/// <summary>
/// Guarantees that the English and Spanish resource tables expose exactly the same keys, so no
/// localized string can ever resolve to a missing translation.
/// </summary>
public class ResourceParityTests
{
    private const string ResourceBaseName = "MediaConverter.App.Resources.Resources";

    private static readonly ResourceManager Manager =
        new(ResourceBaseName, typeof(AppResources).Assembly);

    [Fact]
    public void EnglishAndSpanish_HaveIdenticalKeys()
    {
        var english = ReadKeys(CultureInfo.InvariantCulture);
        var spanish = ReadKeys(new CultureInfo(LanguageService.Spanish));

        var missingInSpanish = english.Except(spanish).OrderBy(key => key).ToArray();
        var missingInEnglish = spanish.Except(english).OrderBy(key => key).ToArray();

        Assert.True(
            missingInSpanish.Length == 0,
            "Keys present in English but missing in Spanish: " + string.Join(", ", missingInSpanish));
        Assert.True(
            missingInEnglish.Length == 0,
            "Keys present in Spanish but missing in English: " + string.Join(", ", missingInEnglish));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("es")]
    public void EveryEntry_HasNonEmptyValue(string language)
    {
        var culture = language == LanguageService.Spanish
            ? new CultureInfo(LanguageService.Spanish)
            : CultureInfo.InvariantCulture;

        // The ResourceManager caches ResourceSets and shares them between calls, so the set must
        // never be disposed here: closing it would poison the cache for later tests.
        var set = Manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

        Assert.NotNull(set);
        foreach (DictionaryEntry entry in set!)
        {
            Assert.False(string.IsNullOrWhiteSpace(entry.Value as string), $"Resource '{entry.Key}' is empty.");
        }
    }

    private static HashSet<string> ReadKeys(CultureInfo culture)
    {
        var set = Manager.GetResourceSet(culture, createIfNotExists: true, tryParents: false);

        Assert.NotNull(set);
        return set!.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).ToHashSet();
    }
}
