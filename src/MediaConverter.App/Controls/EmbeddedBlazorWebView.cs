using System;
using Microsoft.AspNetCore.Components.WebView.Wpf;
using Microsoft.Extensions.FileProviders;

namespace MediaConverter.App.Controls;

/// <summary>
/// A <see cref="BlazorWebView"/> that serves the application's static assets from resources
/// embedded in the assembly instead of the <c>wwwroot</c> folder on disk. This lets the application
/// be published and shipped as a true single self-contained executable, because the host page
/// (<c>wwwroot/index.html</c>) and its CSS and JavaScript are available without any file next to
/// the executable.
/// </summary>
public sealed class EmbeddedBlazorWebView : BlazorWebView
{
    /// <summary>
    /// Creates the file provider that serves the application's static assets. It combines the
    /// embedded assets with the base implementation so that files dropped next to the executable
    /// (and the development-time provider) keep working.
    /// </summary>
    /// <param name="contentRootDir">The on-disk content root directory, normally the <c>wwwroot</c> folder.</param>
    /// <returns>An <see cref="IFileProvider"/> that resolves the embedded assets first.</returns>
    public override IFileProvider CreateFileProvider(string contentRootDir)
    {
        var embedded = new ManifestEmbeddedFileProvider(
            typeof(EmbeddedBlazorWebView).Assembly,
            "wwwroot");

        return new CompositeFileProvider(embedded, base.CreateFileProvider(contentRootDir));
    }
}
