using System.Globalization;
using System.Resources;
using MediaConverter.App.Localization;
using MediaConverter.App.Services;
using AppResources = MediaConverter.App.Resources.Resources;

namespace MediaConverter.App.Tests;

/// <summary>
/// Verifies that the application localizer follows the language selected in
/// <see cref="LanguageService"/> even when the calling thread still has a different UI culture.
/// That is the case at runtime when the language is switched from a thread other than the one that
/// renders the UI, which is what made the in-app language toggle appear to do nothing.
/// </summary>
public class LanguageStringLocalizerTests
{
    private const string ResourceBaseName = "MediaConverter.App.Resources.Resources";

    private static readonly ResourceManager Manager =
        new(ResourceBaseName, typeof(AppResources).Assembly);

    [Fact]
    public void Indexer_UsesLanguageServiceCulture_NotTheThreadCulture()
    {
        var originalCulture = CultureInfo.CurrentUICulture;
        try
        {
            var language = new LanguageService();
            language.SetLanguage(LanguageService.Spanish);

            // Simulate the renderer thread, which is not the thread that switched the language and
            // therefore still carries the previous UI culture.
            CultureInfo.CurrentUICulture = new CultureInfo(LanguageService.English);

            var localizer = new LanguageStringLocalizerFactory(language).Create(typeof(AppResources));

            var expected = Manager.GetString("AudioConverterTitle", new CultureInfo(LanguageService.Spanish));

            Assert.Equal(expected, localizer["AudioConverterTitle"].Value);
        }
        finally
        {
            CultureInfo.CurrentUICulture = originalCulture;
        }
    }

    [Fact]
    public void Indexer_FollowsEveryLanguageChange()
    {
        var language = new LanguageService();
        var localizer = new LanguageStringLocalizerFactory(language).Create(typeof(AppResources));

        language.SetLanguage(LanguageService.English);
        Assert.Equal(
            Manager.GetString("AudioConverterTitle", new CultureInfo(LanguageService.English)),
            localizer["AudioConverterTitle"].Value);

        language.SetLanguage(LanguageService.Spanish);
        Assert.Equal(
            Manager.GetString("AudioConverterTitle", new CultureInfo(LanguageService.Spanish)),
            localizer["AudioConverterTitle"].Value);
    }

    [Fact]
    public void MissingKey_FallsBackToTheKeyName()
    {
        var language = new LanguageService();
        var localizer = new LanguageStringLocalizerFactory(language).Create(typeof(AppResources));

        var result = localizer["ThisKeyDoesNotExist"];

        Assert.True(result.ResourceNotFound);
        Assert.Equal("ThisKeyDoesNotExist", result.Value);
    }
}
