using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Resources;
using MediaConverter.App.Services;
using Microsoft.Extensions.Localization;

namespace MediaConverter.App.Localization;

/// <summary>
/// Creates <see cref="LanguageStringLocalizer"/> instances so the application resolves every
/// localized string with the language selected at runtime instead of the ambient thread culture.
/// Register it in place of the default factory returned by <c>AddLocalization</c>.
/// </summary>
public sealed class LanguageStringLocalizerFactory : IStringLocalizerFactory
{
    private readonly LanguageService _language;
    private readonly ConcurrentDictionary<string, ResourceManager> _resourceManagers = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageStringLocalizerFactory"/> class.
    /// </summary>
    /// <param name="language">The service that exposes the active culture.</param>
    public LanguageStringLocalizerFactory(LanguageService language)
    {
        _language = language ?? throw new ArgumentNullException(nameof(language));
    }

    /// <inheritdoc/>
    public IStringLocalizer Create(Type resourceSource)
    {
        ArgumentNullException.ThrowIfNull(resourceSource);

        var baseName = resourceSource.FullName ?? resourceSource.Name;
        var manager = _resourceManagers.GetOrAdd(
            baseName + "|" + resourceSource.Assembly.FullName,
            _ => new ResourceManager(baseName, resourceSource.Assembly));

        return new LanguageStringLocalizer(manager, _language);
    }

    /// <inheritdoc/>
    public IStringLocalizer Create(string baseName, string location)
    {
        ArgumentNullException.ThrowIfNull(baseName);
        ArgumentNullException.ThrowIfNull(location);

        var manager = _resourceManagers.GetOrAdd(
            baseName + "|" + location,
            _ => new ResourceManager(baseName, Assembly.Load(new AssemblyName(location))));

        return new LanguageStringLocalizer(manager, _language);
    }
}
