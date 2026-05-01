# Image Renamr

Image Renamr is a cross-platform desktop app for renaming entire folders of images without turning the task into a script. Built with Avalonia on .NET, it focuses on a practical batch workflow: choose a source folder, pick an output folder, set a prefix, decide how conflicts should be handled, and export renamed copies with live progress feedback.

![Illustrated header for ImageRenamr](docs/header.jpg)

The project is aimed at everyday image-organization jobs such as preparing travel photos, normalizing mixed camera exports, cleaning up filenames before archival, or producing consistently named handoff folders for clients and teams.

## Highlights

- Batch rename supported images from a desktop UI
- Copy files into a separate output folder with predictable numbering
- Use a shared prefix with automatic zero-padded sequence numbers
- Preserve the original file format of every copied image
- Skip or overwrite existing files in the output folder
- Track progress through live status updates and completion summaries
- Support `.jpg`, `.jpeg`, `.png`, `.gif`, `.bmp`, `.tif`, `.tiff`, `.webp`, and `.avif`

> Note: the current rename workflow processes files from the selected input folder only. It does not recurse into nested subfolders.

## Screenshots

### Batch setup

![ImageRenamr batch setup screen](docs/screenshot-01.png)

The main screen keeps the workflow simple: select an input folder, choose an output folder, enter a prefix, and decide whether existing files should be overwritten.

### Live progress during a running batch

![ImageRenamr live progress while renaming images](docs/screenshot-02.png)

While the batch is running, Image Renamr shows overall progress, the latest file activity, and a clear status message so you can see what the app is doing at a glance.

### Completion summary

![ImageRenamr completion summary after renaming images](docs/screenshot-03.png)

After the batch finishes, the same workspace shows the final summary, including copied and skipped files, without forcing you into a separate report view.

## Rename Workflow

1. Select the folder that contains the source images.
2. Choose where the renamed copies should be written.
3. Enter the prefix that should be used for the generated file names.
4. Decide whether to skip or overwrite existing output files.
5. Start the batch and follow the live progress panel.

## Supported Formats

Image Renamr currently accepts the following input file types:

- `.jpg`
- `.jpeg`
- `.png`
- `.gif`
- `.bmp`
- `.tif`
- `.tiff`
- `.webp`
- `.avif`

Renamed output files keep the original file extension and are written as copies into the selected output folder.

## Getting Started

### Prerequisites

- .NET SDK matching the version pinned in `global.json`

### Restore

```bash
dotnet restore ImageRenamr.slnx
```

### Build

```bash
dotnet build ImageRenamr.slnx --configuration Release
```

### Run the desktop app

```bash
dotnet run --project src/ImageRenamr.App/ImageRenamr.App.csproj --configuration Release
```

### Run the tests

```bash
dotnet test ImageRenamr.slnx --configuration Release
```

There is no separate lint command. Repository analyzers and code-style checks run as part of the build.

## Project Structure

- `src/ImageRenamr.App`: Avalonia desktop UI shell
- `src/ImageRenamr.Core`: rename pipeline, models, and file-handling logic
- `tests/ImageRenamr.Tests`: xUnit tests for the rename service and the window view model

The solution intentionally keeps the UI thin. Avalonia-specific code lives in the app project, while the rename workflow, validation, file handling, and progress reporting live in the core library.

## Tech Stack

- [.NET](https://dotnet.microsoft.com/)
- [Avalonia UI](https://avaloniaui.net/)
- [Semi.Avalonia](https://github.com/irihitech/Semi.Avalonia)
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/)
- [xUnit](https://xunit.net/)

## Quality

The repository includes automated coverage for both the batch rename service and the window view model. Tests verify deterministic ordering, file handling, overwrite rules, prefix validation, progress reporting, and completion-state behavior.

## License

This project is licensed under the MIT License. See [LICENSE](LICENSE) for details.
