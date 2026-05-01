using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ImageRenamr.App.ViewModels;
using ImageRenamr.App.Views;
using ImageRenamr.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ImageRenamr.App;

public partial class App : Application
{
    /// <summary>
    /// Provides access to the application's dependency injection service provider instance.
    /// </summary>
    /// <remarks>This field holds a reference to the root service provider used for resolving application
    /// services. It may be null if the service provider has not been initialized.</remarks>
    private ServiceProvider? serviceProvider;

    /// <summary>
    /// Initializes the components defined in the associated XAML for this class.
    /// </summary>
    /// <remarks>This method is typically called by the framework to load and connect the XAML-defined UI
    /// elements to their corresponding code-behind. It should not be called directly in most scenarios.</remarks>
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// Performs application-specific initialization logic after the Avalonia framework has completed its initialization
    /// process.
    /// </summary>
    /// <remarks>This method is typically used to configure services, set up the main window, and attach event
    /// handlers before the application starts running. It is called automatically by the Avalonia framework and should
    /// not be invoked directly.</remarks>
    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            serviceProvider = ConfigureServices();
            desktop.MainWindow = serviceProvider.GetRequiredService<MainWindow>();
            desktop.Exit += DesktopOnExit;
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Configures and builds the application's dependency injection service provider.
    /// </summary>
    /// <remarks>Registers core services and view models required for the application's operation. The
    /// returned service provider is configured with scope validation enabled to help detect misconfigured service
    /// lifetimes during development.</remarks>
    /// <returns>A <see cref="ServiceProvider"/> instance containing the registered application services.</returns>
    private static ServiceProvider ConfigureServices()
    {
        ServiceCollection services = new();

        // Core services
        _ = services.AddSingleton<IImageRenamrService, ImageRenamrService>();

        // ViewModels and UI
        _ = services.AddSingleton<MainWindowViewModel>();
        _ = services.AddSingleton<MainWindow>();

        return services.BuildServiceProvider(validateScopes: true);
    }

    /// <summary>
    /// Handles the application exit event for desktop environments, performing necessary cleanup of resources.
    /// </summary>
    /// <remarks>This method is intended to be attached to the exit event of a desktop application's
    /// controlled lifetime. It ensures that any resources managed by the service provider are properly disposed when
    /// the application is shutting down.</remarks>
    /// <param name="sender">The source of the event, typically the application lifetime object.</param>
    /// <param name="e">An object that contains the event data for the application exit event.</param>
    private void DesktopOnExit(object? sender, ControlledApplicationLifetimeExitEventArgs e)
    {
        serviceProvider?.Dispose();
        serviceProvider = null;
    }
}
