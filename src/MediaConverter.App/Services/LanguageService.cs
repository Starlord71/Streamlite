using System;
using System.Globalization;
using System.IO;
using System.Text.Json;
using MediaConverter.Core.Services.Binaries;

namespace MediaConverter.App.Services;

/// <summary>
/// Owns the active UI language of the application. It applies the matching
/// <see cref="CultureInfo"/>, persists the user preference to <c>settings.json</c> next to the
/// executable, and raises <see cref="LanguageChanged"/> so components can re-render when the
/// language switches.
/// </summary>
public sealed class LanguageService
{
    /// <summary>Language code for English.</summary>
    public const string English = "en";

    /// <summary>Language code for Spanish.</summary>
    public const string Spanish = "es";

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
    };

    private readonly string _settingsPath;

    /// <summary>
    /// Builds the service, loads any saved preference from <c>settings.json</c>, and falls back to
    /// the system language when there is no saved preference or the file cannot be read.
    /// </summary>
    public LanguageService()
    {
        _settingsPath = GetSettingsPath();

        var savedLanguage = ReadSavedLanguage(_settingsPath);
        if (savedLanguage is not null)
        {
            HasSavedPreference = true;
            CurrentLanguage = savedLanguage;
        }
        else
        {
            HasSavedPreference = false;
            CurrentLanguage = ResolveSystemDefaultLanguage();
        }

        CurrentCulture = new CultureInfo(CurrentLanguage);
    }

    /// <summary>
    /// Raised after the active language changes, so listeners (Razor components, the WPF window)
    /// can re-render localized text.
    /// </summary>
    public event EventHandler? LanguageChanged;

    /// <summary>Gets the active language code (<see cref="English"/> or <see cref="Spanish"/>).</summary>
    public string CurrentLanguage { get; private set; }

    /// <summary>Gets the <see cref="CultureInfo"/> that matches <see cref="CurrentLanguage"/>.</summary>
    public CultureInfo CurrentCulture { get; private set; }

    /// <summary>
    /// Gets a value indicating whether the user has already chosen a language that is persisted in
    /// <c>settings.json</c>. When <see langword="false"/>, the app must show the first-run language
    /// picker before provisioning.
    /// </summary>
    public bool HasSavedPreference { get; private set; }

    /// <summary>
    /// Applies the current culture to the process. Called once at startup, before the UI is built,
    /// so the very first render already uses the correct language.
    /// </summary>
    public void Initialize() => ApplyCulture(CurrentCulture);

    /// <summary>
    /// Switches the active language, applies its culture, persists the preference and notifies
    /// listeners. Unknown codes fall back to <see cref="English"/>.
    /// </summary>
    /// <param name="languageCode">The language code to activate.</param>
    public void SetLanguage(string languageCode)
    {
        var normalized = TryNormalize(languageCode) ?? English;
        var culture = new CultureInfo(normalized);

        CurrentLanguage = normalized;
        CurrentCulture = culture;
        ApplyCulture(culture);
        HasSavedPreference = true;

        PersistLanguage(normalized);

        LanguageChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Resolves the default language from the operating system UI culture: any <c>es*</c> culture
    /// maps to <see cref="Spanish"/> and everything else maps to <see cref="English"/>.
    /// </summary>
    /// <returns>The default language code.</returns>
    public static string ResolveSystemDefaultLanguage()
    {
        var twoLetterName = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;
        return string.Equals(twoLetterName, Spanish, StringComparison.OrdinalIgnoreCase)
            ? Spanish
            : English;
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        // DefaultThreadCurrent* covers the Blazor renderer thread (which we do not own), while the
        // Current* pair makes the change immediate on the calling thread.
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static string GetSettingsPath() =>
        Path.Combine(BinaryPaths.GetExecutableDirectory(), "settings.json");

    private static string? ReadSavedLanguage(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                return null;
            }

            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<AppSettings>(json, SerializerOptions);
            return TryNormalize(settings?.Language);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            // A missing or corrupt settings file is not fatal: fall back to the system language.
            return null;
        }
    }

    private void PersistLanguage(string languageCode)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(
                new AppSettings { Language = languageCode },
                SerializerOptions);

            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Persistence is best-effort; the in-memory culture still applies for this session.
        }
    }

    private static string? TryNormalize(string? languageCode)
    {
        if (string.IsNullOrWhiteSpace(languageCode))
        {
            return null;
        }

        if (languageCode.StartsWith(Spanish, StringComparison.OrdinalIgnoreCase))
        {
            return Spanish;
        }

        if (languageCode.StartsWith(English, StringComparison.OrdinalIgnoreCase))
        {
            return English;
        }

        return null;
    }

    /// <summary>
    /// Shape of the persisted settings file. Only the language is stored today; later phases can
    /// extend it with more preferences.
    /// </summary>
    private sealed record AppSettings
    {
        /// <summary>Gets the persisted language code, or <see langword="null"/> when unset.</summary>
        public string? Language { get; init; }
    }
}
