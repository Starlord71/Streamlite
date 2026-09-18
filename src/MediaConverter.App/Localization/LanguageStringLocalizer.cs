using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Resources;
using MediaConverter.App.Services;
using Microsoft.Extensions.Localization;

namespace MediaConverter.App.Localization;

/// <summary>
/// Resolves resource strings with the language selected in <see cref="LanguageService"/> instead of
/// the ambient <see cref="CultureInfo.CurrentUICulture"/> of the calling thread. The framework
/// localizer reads the thread culture at lookup time, but in a Blazor hybrid app the language can be
/// switched from a thread other than the one that renders the component, so a change would
/// otherwise not be reflected until the next restart.
/// </summary>
public sealed class LanguageStringLocalizer : IStringLocalizer
{
    private readonly ResourceManager _resourceManager;
    private readonly LanguageService _language;

    /// <summary>
    /// Initializes a new instance of the <see cref="LanguageStringLocalizer"/> class.
    /// </summary>
    /// <param name="resourceManager">The resource manager that owns the localized resources.</param>
    /// <param name="language">The service that exposes the active culture.</param>
    public LanguageStringLocalizer(ResourceManager resourceManager, LanguageService language)
    {
        _resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
        _language = language ?? throw new ArgumentNullException(nameof(language));
    }

    /// <inheritdoc/>
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var value = _resourceManager.GetString(name, _language.CurrentCulture);
            return new LocalizedString(
                name,
                value ?? name,
                resourceNotFound: value is null,
                searchedLocation: _resourceManager.BaseName);
        }
    }

    /// <inheritdoc/>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            var format = _resourceManager.GetString(name, _language.CurrentCulture);
            var value = string.Format(_language.CurrentCulture, format ?? name, arguments);
            return new LocalizedString(
                name,
                value,
                resourceNotFound: format is null,
                searchedLocation: _resourceManager.BaseName);
        }
    }

    /// <inheritdoc/>
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
    {
        var resourceSet = _resourceManager.GetResourceSet(
            _language.CurrentCulture,
            createIfNotExists: true,
            tryParents: includeParentCultures);

        if (resourceSet is null)
        {
            yield break;
        }

        foreach (DictionaryEntry entry in resourceSet)
        {
            if (entry.Key is string key && entry.Value is string value)
            {
                yield return new LocalizedString(
                    key,
                    value,
                    resourceNotFound: false,
                    searchedLocation: _resourceManager.BaseName);
            }
        }
    }
}
