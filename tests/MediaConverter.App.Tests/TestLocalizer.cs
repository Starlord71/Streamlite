using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using AppResources = MediaConverter.App.Resources.Resources;

namespace MediaConverter.App.Tests;

/// <summary>
/// Builds an <see cref="IStringLocalizer"/> backed by the application resources for a given
/// language. Tests use it to exercise the real localization pipeline instead of a stub.
/// </summary>
internal static class TestLocalizer
{
    /// <summary>
    /// Creates a localizer resolved against the App resources for <paramref name="language"/>.
    /// </summary>
    /// <param name="language">The language code (<c>en</c> or <c>es</c>) to activate.</param>
    /// <returns>The localizer used by the application at runtime.</returns>
    public static IStringLocalizer Create(string language)
    {
        var culture = new CultureInfo(language);
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();

        var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IStringLocalizerFactory>();
        return factory.Create(typeof(AppResources));
    }
}
