using Avalonia;

namespace ImageRenamr.App;

/// <summary>
/// Provides the application entry point and configures the Avalonia application for desktop environments.
/// </summary>
/// <remarks>This class contains the Main method, which initializes and starts the Avalonia application using a
/// classic desktop lifetime. It also provides the method to configure Avalonia with platform detection and logging. No
/// Avalonia or third-party APIs should be used before the application is fully initialized by AppMain.</remarks>
internal sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
