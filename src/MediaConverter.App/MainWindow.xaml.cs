using System;
using System.Windows;

namespace MediaConverter.App;

/// <summary>
/// Main application window. Hosts the <c>BlazorWebView</c> that renders the Razor components and
/// exposes the application's dependency injection container to them.
/// </summary>
public partial class MainWindow : Window
{
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
    }
}
