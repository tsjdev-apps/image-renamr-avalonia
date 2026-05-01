# Copilot Instructions

## Project

Cross-platform desktop application for renaming image files within a specified folder. Built with C# and Avalonia UI targeting .NET, runnable on Windows, macOS, and Linux.

## Tech Stack

- **Language:** C#
- **UI Framework:** [Avalonia](https://avaloniaui.net/) (cross-platform desktop)
- **Pattern:** MVVM (Model-View-ViewModel)
- **Package Manager:** NuGet
- **Build System:** MSBuild via .NET CLI

## Build & Run

```bash
# Restore dependencies
dotnet restore

# Build
dotnet build

# Run the application
dotnet run --project src/ImageRenamr.App/ImageRenamr.App.csproj
```

## Testing

```bash
# Run all tests
dotnet test

# Run a single test
dotnet test --filter "FullyQualifiedName~MyTestClass.MyTestMethod"

# Run tests in a specific project
dotnet test tests/ImageRenamr.Tests/ImageRenamr.Tests.csproj
```

## Architecture

```
src/
  ImageRenamr.App/        # Main application project
    ViewModels/           # MVVM ViewModels (bind to Views)
    Views/                # Avalonia AXAML views
    App.axaml             # Application entry point
tests/
  ImageRenamr.Tests/      # Unit/integration tests
```

- **ViewModels** must not reference Avalonia UI types directly — keep them testable.
- Avalonia views use `.axaml` (not `.xaml`) and code-behind is kept minimal.

## Key Conventions

- Follow standard .NET naming: `PascalCase` for types/methods, `camelCase` for locals, `_camelCase` for private fields.
- ViewModels implement `INotifyPropertyChanged` — prefer `CommunityToolkit.Mvvm` source generators (`[ObservableProperty]`, `[RelayCommand]`).
- Dependency injection via `Microsoft.Extensions.DependencyInjection`; register services in `App.axaml.cs`.
- Image file handling targets common formats: JPEG, PNG, GIF, BMP, WebP.
