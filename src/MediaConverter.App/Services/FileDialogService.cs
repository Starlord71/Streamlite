using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.JSInterop;
using Microsoft.Win32;

namespace MediaConverter.App.Services;

/// <summary>
/// Native Windows file and folder dialog implementation for the Razor UI. The Razor component
/// talks to the host through JavaScript interop because a browser file input cannot return a real
/// filesystem path, which is what ffmpeg and yt-dlp need. JavaScript only forwards the request back
/// to the WPF host, where <see cref="OpenFileDialog"/> or <see cref="OpenFolderDialog"/> runs on the
/// application dispatcher.
/// </summary>
public sealed class FileDialogService : IFileDialogService, IDisposable
{
    private const string SetHostBridgeFunction = "fileDialog.setHostBridge";
    private const string OpenFilePickerFunction = "fileDialog.openFilePicker";
    private const string OpenFolderPickerFunction = "fileDialog.openFolderPicker";

    private readonly IJSRuntime _js;
    private DotNetObjectReference<FileDialogService>? _hostBridge;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileDialogService"/> class.
    /// </summary>
    /// <param name="js">The JavaScript runtime used to reach the WPF host through the WebView.</param>
    public FileDialogService(IJSRuntime js)
    {
        _js = js;
    }

    /// <inheritdoc/>
    public async Task<string?> PickFileAsync(string title, string filter)
    {
        if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(filter))
        {
            return null;
        }

        // The bridge is registered once and reused for every pick. Registering it lazily keeps the
        // constructor free of JavaScript calls, which cannot run before the WebView is ready.
        _hostBridge ??= DotNetObjectReference.Create(this);

        await _js.InvokeVoidAsync(SetHostBridgeFunction, _hostBridge);
        return await _js.InvokeAsync<string?>(OpenFilePickerFunction, filter, title);
    }

    /// <inheritdoc/>
    public async Task<string?> PickFolderAsync(string title)
    {
        if (string.IsNullOrEmpty(title))
        {
            return null;
        }

        // Same shared bridge as PickFileAsync: one registration serves both pickers.
        _hostBridge ??= DotNetObjectReference.Create(this);

        await _js.InvokeVoidAsync(SetHostBridgeFunction, _hostBridge);
        return await _js.InvokeAsync<string?>(OpenFolderPickerFunction, title);
    }

    /// <summary>
    /// Shows the native open-file dialog. Called from JavaScript through the registered bridge;
    /// the dialog always runs on the WPF application dispatcher.
    /// </summary>
    /// <param name="filter">Win32 filter string passed to the dialog.</param>
    /// <param name="title">Localized dialog title.</param>
    /// <returns>The selected absolute path, or <see langword="null"/> when the user cancels.</returns>
    [JSInvokable]
    public Task<string?> ShowOpenFileDialogAsync(string filter, string title)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return Task.FromResult<string?>(null);
        }

        return dispatcher.InvokeAsync(() =>
        {
            var dialog = new OpenFileDialog
            {
                Title = title,
                Filter = filter,
                CheckFileExists = true,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }).Task;
    }

    /// <summary>
    /// Shows the native folder-picker dialog. Called from JavaScript through the registered bridge;
    /// the dialog always runs on the WPF application dispatcher.
    /// </summary>
    /// <param name="title">Localized dialog title.</param>
    /// <returns>The selected absolute directory path, or <see langword="null"/> when the user cancels.</returns>
    [JSInvokable]
    public Task<string?> ShowOpenFolderDialogAsync(string title)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null)
        {
            return Task.FromResult<string?>(null);
        }

        return dispatcher.InvokeAsync(() =>
        {
            var dialog = new OpenFolderDialog
            {
                Title = title,
                Multiselect = false,
            };

            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }).Task;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        _hostBridge?.Dispose();
    }
}
