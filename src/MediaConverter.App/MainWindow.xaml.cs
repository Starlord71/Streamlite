using System;
using System.Windows;
using MediaConverter.App.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using AppResources = MediaConverter.App.Resources.Resources;

namespace MediaConverter.App;

/// <summary>
/// Main application window. Hosts the <c>BlazorWebView</c> that renders the Razor components,
/// exposes the application's dependency injection container to them and keeps its title localized.
/// </summary>
public partial class MainWindow : Window
{
    private readonly IStringLocalizer<AppResources> _localizer;
    private readonly LanguageService _language;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow"/> class.
    /// </summary>
    /// <param name="services">
    /// The application service provider. Blazor resolves component dependencies from it.
    /// </param>
    public MainWindow(IServiceProvider services)
    {
        InitializeComponent();
        RootWebView.Services = services;

        _localizer = services.GetRequiredService<IStringLocalizer<AppResources>>();
        _language = services.GetRequiredService<LanguageService>();

        Title = _localizer["AppTitle"];
        _language.LanguageChanged += OnLanguageChanged;
    }

    /// <inheritdoc/>
    protected override void OnClosed(EventArgs e)
    {
        _language.LanguageChanged -= OnLanguageChanged;
        base.OnClosed(e);
    }

    private void OnLanguageChanged(object? sender, EventArgs e) => Title = _localizer["AppTitle"];
}
