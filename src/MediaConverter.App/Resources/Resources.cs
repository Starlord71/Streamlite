namespace MediaConverter.App.Resources;

/// <summary>
/// Marker type used to resolve the application string resources through
/// <see cref="Microsoft.Extensions.Localization.IStringLocalizer{T}"/>. Its full name intentionally
/// matches the embedded resource base name generated from <c>Resources.resx</c>
/// (<c>MediaConverter.App.Resources.Resources</c>), so the default localizer factory finds it
/// without any custom resource path configuration.
/// </summary>
public sealed class Resources
{
}
