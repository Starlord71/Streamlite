using System;
using System.Windows;
using MediaConverter.App.Services;
using MediaConverter.Core.Interfaces;
using MediaConverter.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MediaConverter.App;

/// <summary>
/// WPF application entry point. Builds the dependency injection container that backs the
/// Blazor components, hosts the main window and disposes the container (and therefore all
/// disposable singletons) when the application shuts down.
/// </summary>
public partial class App : Application
{
    private ServiceProvider? _serviceProvider;

    /// <inheritdoc/>
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _serviceProvider = BuildServiceProvider();

        // Apply the saved (or system-default) culture before any UI exists, so the first render and
        // the window title are already localized.
        _serviceProvider.GetRequiredService<LanguageService>().Initialize();

        var mainWindow = new MainWindow(_serviceProvider);
        MainWindow = mainWindow;
        mainWindow.Show();
    }

    /// <inheritdoc/>
    protected override void OnExit(ExitEventArgs e)
    {
        _serviceProvider?.Dispose();
        base.OnExit(e);
    }

    /// <summary>
    /// Registers the BlazorWebView infrastructure required to render the components, the Core
    /// services, the localization services and the App-level <see cref="LanguageService"/>. Only
    /// <see cref="IBinariesProvisioningService"/> is used by the UI in this phase; the audio and
    /// video services are registered now so later phases can inject them directly.
    /// <see cref="BinariesProvisioningService"/> is a singleton because it owns a long-lived
    /// <see cref="System.Net.Http.HttpClient"/> that must be reused and disposed.
    /// </summary>
    /// <returns>The configured <see cref="ServiceProvider"/>.</returns>
    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();

        // AddWpfBlazorWebView registers the WebView2WebViewManager factory that the WPF
        // BlazorWebView resolves from the container when it applies its control template. Without
        // it, applying the template throws InvalidOperationException and the window never opens.
        services.AddWpfBlazorWebView();

        // AddLocalization registers IStringLocalizerFactory and IStringLocalizer<T>. Its factory
        // depends on ILoggerFactory, so logging must be registered as well. The default resource
        // base name is the marker type's full name (MediaConverter.App.Resources.Resources),
        // which matches Resources.resx.
        services.AddLogging();
        services.AddLocalization();
        services.AddSingleton<LanguageService>();
        services.AddSingleton<IFileDialogService, FileDialogService>();
        services.AddSingleton<OperationCoordinator>();

        services.AddSingleton<IBinariesProvisioningService, BinariesProvisioningService>();
        services.AddSingleton<IAudioConverterService, AudioConverterService>();
        services.AddSingleton<IVideoDownloaderService, VideoDownloaderService>();

        return services.BuildServiceProvider();
    }
}
