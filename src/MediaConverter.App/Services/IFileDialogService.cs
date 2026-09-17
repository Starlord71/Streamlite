namespace MediaConverter.App.Services;

/// <summary>
/// Opens native Windows file dialogs on behalf of the Razor UI. The interface exposes no WPF type
/// so Razor components can request a picker without referencing the WPF host; the implementation
/// lives in the App layer and bridges to the host through JavaScript interop.
/// </summary>
public interface IFileDialogService
{
    /// <summary>
    /// Shows the native open-file dialog and returns the selected file path.
    /// </summary>
    /// <param name="title">Localized dialog title.</param>
    /// <param name="filter">Win32 filter string, for example <c>Audio files (*.m4a;*.mp3)|*.m4a;*.mp3</c>.</param>
    /// <returns>The selected absolute path, or <see langword="null"/> when the user cancels.</returns>
    Task<string?> PickFileAsync(string title, string filter);
}
