using MediaConverter.App.Localization;
using MediaConverter.App.Services;
using MediaConverter.Core.Models;

namespace MediaConverter.App.Tests;

/// <summary>
/// Guarantees that every machine-readable code Core can return is rendered through a friendly,
/// localized message in both supported languages, never as a raw enum value.
/// </summary>
public class ErrorLocalizationTests
{
    [Theory]
    [InlineData(LanguageService.English)]
    [InlineData(LanguageService.Spanish)]
    public void DescribeErrorCode_EveryDefinedCode_ReturnsLocalizedMessage(string language)
    {
        var localizer = TestLocalizer.Create(language);

        foreach (var code in Enum.GetValues<ErrorCode>())
        {
            var message = localizer.DescribeErrorCode(code);

            Assert.False(string.IsNullOrWhiteSpace(message));
            Assert.DoesNotContain($"({(int)code})", message);
            Assert.DoesNotContain(code.ToString(), message);
        }
    }

    [Theory]
    [InlineData(LanguageService.English)]
    [InlineData(LanguageService.Spanish)]
    public void DescribeErrorCode_UndefinedCode_FallsBackToMessageThatStillNamesTheCode(string language)
    {
        var localizer = TestLocalizer.Create(language);

        var message = localizer.DescribeErrorCode((ErrorCode)(-1));

        Assert.Contains("(-1)", message);
    }

    [Theory]
    [InlineData(LanguageService.English)]
    [InlineData(LanguageService.Spanish)]
    public void DescribeStage_EveryStage_ReturnsLocalizedMessage(string language)
    {
        var localizer = TestLocalizer.Create(language);

        foreach (var stage in Enum.GetValues<ProgressStage>())
        {
            Assert.False(string.IsNullOrWhiteSpace(localizer.DescribeStage(stage)));
        }

        Assert.False(string.IsNullOrWhiteSpace(localizer.DescribeStage(null)));
    }

    [Theory]
    [InlineData(LanguageService.English)]
    [InlineData(LanguageService.Spanish)]
    public void DescribeErrorCode_KnownCode_ResolvesTheMatchingResource(string language)
    {
        var localizer = TestLocalizer.Create(language);

        Assert.Equal(localizer["ErrorNetworkError"].Value, localizer.DescribeErrorCode(ErrorCode.NetworkError));
        Assert.Equal(localizer["ErrorCancelled"].Value, localizer.DescribeErrorCode(ErrorCode.Cancelled));
        Assert.Equal(localizer["StageDownloading"].Value, localizer.DescribeStage(ProgressStage.Downloading));
    }

    [Fact]
    public void DescribeErrorCode_EnglishAndSpanish_ReturnDifferentText()
    {
        var english = TestLocalizer.Create(LanguageService.English).DescribeErrorCode(ErrorCode.NetworkError);
        var spanish = TestLocalizer.Create(LanguageService.Spanish).DescribeErrorCode(ErrorCode.NetworkError);

        Assert.NotEqual(english, spanish);
    }
}
