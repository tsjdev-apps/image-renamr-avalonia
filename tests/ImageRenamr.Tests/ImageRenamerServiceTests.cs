using ImageRenamr.Core.Models;
using ImageRenamr.Core.Services;

namespace ImageRenamr.Tests;

/// <summary>
/// Contains unit tests for the ImageRenamerService class, verifying the behavior of the RenameAsync method under various
/// </summary>
public class ImageRenamerServiceTests
{
    /// <summary>
    /// Verifies that the RenameAsync method copies supported image files from the input folder to the output folder in
    /// deterministic alphabetical order, applying the specified naming pattern.
    /// </summary>
    /// <remarks>This test ensures that only supported image files are processed, non-image files are ignored,
    /// and the output files are named sequentially in alphabetical order based on the original filenames. The test also
    /// verifies that the file contents are preserved and that the correct number of files are copied and
    /// skipped.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncCopiesSupportedImagesInDeterministicAlphabeticalOrder()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("zebra.png", "zebra");
        _ = workspace.CreateInputFile("Ant.JPG", "ant");
        _ = workspace.CreateInputFile("notes.txt", "ignore me");

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "TRIP",
                    OverwriteExisting: false),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(2, result.TotalFiles);
        Assert.Equal(2, result.CopiedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.Equal("ant", await File.ReadAllTextAsync(Path.Combine(workspace.OutputFolder, "TRIP_01.JPG"), TestContext.Current.CancellationToken));
        Assert.Equal("zebra", await File.ReadAllTextAsync(Path.Combine(workspace.OutputFolder, "TRIP_02.png"), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that the RenameAsync method skips files with naming conflicts when overwriting is disabled.
    /// </summary>
    /// <remarks>This test ensures that when a file with the target name already exists in the output folder
    /// and the overwrite option is set to false, the existing file is not replaced and the operation is counted as
    /// skipped.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncSkipsConflictsWhenOverwriteIsDisabled()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("camera.jpg", "new content");
        _ = workspace.CreateOutputFile("TRIP_01.jpg", "existing content");

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "TRIP",
                    OverwriteExisting: false),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(0, result.CopiedFiles);
        Assert.Equal(1, result.SkippedFiles);
        Assert.Equal("existing content", await File.ReadAllTextAsync(Path.Combine(workspace.OutputFolder, "TRIP_01.jpg"), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that the RenameAsync method overwrites existing files in the output folder when the OverwriteExisting
    /// option is enabled.
    /// </summary>
    /// <remarks>This test ensures that when a file with the target name already exists in the output
    /// directory, and the overwrite option is set to true, the existing file is replaced with the new content. The test
    /// checks that the file count, copied count, and skipped count reflect the overwrite behavior, and that the file
    /// content matches the input.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncOverwritesConflictsWhenOverwriteIsEnabled()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("camera.jpg", "fresh content");
        _ = workspace.CreateOutputFile("TRIP_01.jpg", "existing content");

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "TRIP",
                    OverwriteExisting: true),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.TotalFiles);
        Assert.Equal(1, result.CopiedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.Equal("fresh content", await File.ReadAllTextAsync(Path.Combine(workspace.OutputFolder, "TRIP_01.jpg"), TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// Verifies that the RenameAsync method throws an ArgumentException when the input and output folders are the same.
    /// </summary>
    /// <remarks>This test ensures that the service enforces the requirement for distinct input and output
    /// folders when renaming images.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncRejectsUsingTheSameInputAndOutputFolder()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("camera.jpg", "content");

        ImageRenamrService service = new();

        ArgumentException exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.RenameAsync(
                    new RenameImagesRequest(
                        workspace.InputFolder,
                        workspace.InputFolder,
                        "TRIP",
                        OverwriteExisting: false),
                    cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("Choose different input and output folders", exception.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// Verifies that the RenameAsync method uses three-digit numeric padding for output file names when renaming a
    /// batch of more than ninety-nine image files.
    /// </summary>
    /// <remarks>This test ensures that when one hundred image files are processed, the resulting file names
    /// are padded with three digits (e.g., SET_001.jpg to SET_100.jpg), confirming correct filename formatting for
    /// large batches.</remarks>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncUsesThreeDigitPaddingWhenTheBatchExceedsNinetyNineFiles()
    {
        using TestWorkspace workspace = new();

        for (int index = 1; index <= 100; index++)
        {
            _ = workspace.CreateInputFile($"image-{index:D3}.jpg", $"content-{index}");
        }

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "SET",
                    OverwriteExisting: false),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(100, result.TotalFiles);
        Assert.Equal(100, result.CopiedFiles);
        Assert.True(File.Exists(Path.Combine(workspace.OutputFolder, "SET_001.jpg")));
        Assert.True(File.Exists(Path.Combine(workspace.OutputFolder, "SET_100.jpg")));
    }

    /// <summary>
    /// Verifies that the RenameAsync method trims surrounding whitespace from the prefix before creating output names.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncTrimsPrefixBeforeBuildingDestinationNames()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("camera.jpg", "content");

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "  SET  ",
                    OverwriteExisting: false),
                cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(1, result.CopiedFiles);
        Assert.True(File.Exists(Path.Combine(workspace.OutputFolder, "SET_01.jpg")));
    }

    /// <summary>
    /// Verifies that the RenameAsync method returns an empty result when no supported images are present.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncReturnsEmptyResultWhenNoSupportedImagesAreFound()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("notes.txt", "ignore me");
        RecordingProgress progress = new();

        ImageRenamrService service = new();

        RenameImagesResult result =
            await service.RenameAsync(
                new RenameImagesRequest(
                    workspace.InputFolder,
                    workspace.OutputFolder,
                    "SET",
                    OverwriteExisting: false),
                progress,
                TestContext.Current.CancellationToken);

        Assert.Equal(0, result.TotalFiles);
        Assert.Equal(0, result.CopiedFiles);
        Assert.Equal(0, result.SkippedFiles);
        Assert.Empty(result.OutputFiles);
        Assert.Contains(progress.Messages, message => message.Contains("No supported image files", StringComparison.Ordinal));
    }

    /// <summary>
    /// Verifies that invalid file-name characters in a prefix are rejected before copying files.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task RenameAsyncRejectsPrefixWithInvalidFileNameCharacters()
    {
        using TestWorkspace workspace = new();
        _ = workspace.CreateInputFile("camera.jpg", "content");
        char invalidCharacter = Path.GetInvalidFileNameChars().First(character => character != '\0');

        ImageRenamrService service = new();

        ArgumentException exception =
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.RenameAsync(
                    new RenameImagesRequest(
                        workspace.InputFolder,
                        workspace.OutputFolder,
                        $"BAD{invalidCharacter}NAME",
                        OverwriteExisting: false),
                    cancellationToken: TestContext.Current.CancellationToken));

        Assert.Contains("not valid in file names", exception.Message, StringComparison.Ordinal);
    }

    private sealed class RecordingProgress : IProgress<RenameProgressUpdate>
    {
        public List<string> Messages { get; } = [];

        public void Report(RenameProgressUpdate value)
        {
            Messages.Add(value.Message);
        }
    }

    /// <summary>
    /// Provides a temporary workspace with isolated input and output directories for use in tests.
    /// </summary>
    /// <remarks>The workspace creates unique temporary folders for each instance, ensuring test isolation.
    /// All files and directories created within the workspace are deleted when the instance is disposed. This class is
    /// intended for use in automated tests that require file system operations.</remarks>
    private sealed class TestWorkspace : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the TestWorkspace class, creating unique temporary input and output
        /// directories for use in test scenarios.
        /// </summary>
        /// <remarks>The created directories are located within the system's temporary folder and are
        /// uniquely named for each instance to avoid conflicts between tests. This constructor is intended for use in
        /// automated testing environments where isolated file system workspaces are required.</remarks>
        public TestWorkspace()
        {
            RootFolder = Path.Combine(Path.GetTempPath(), "ImageRenamer.Tests", Guid.NewGuid().ToString("N"));
            InputFolder = Path.Combine(RootFolder, "input");
            OutputFolder = Path.Combine(RootFolder, "output");

            _ = Directory.CreateDirectory(InputFolder);
            _ = Directory.CreateDirectory(OutputFolder);
        }

        /// <summary>
        /// Gets the full path of the root folder used for image file operations.
        /// </summary>
        public string RootFolder { get; }

        /// <summary>
        /// Gets the full path of the folder containing the input images to be processed.
        /// </summary>
        public string InputFolder { get; }

        /// <summary>
        /// Gets the path to the folder where renamed image files will be saved.
        /// </summary>
        public string OutputFolder { get; }

        /// <summary>
        /// Releases all resources used by the current instance and deletes the root folder and its contents, if it
        /// exists.
        /// </summary>
        /// <remarks>This method deletes the directory specified by the RootFolder property, including all
        /// files and subdirectories. After calling this method, the root folder and its contents will no longer be
        /// available. Use caution to avoid unintentional data loss.</remarks>
        public void Dispose()
        {
            if (Directory.Exists(RootFolder))
            {
                Directory.Delete(RootFolder, recursive: true);
            }
        }

        /// <summary>
        /// Creates a new file with the specified name and writes the provided content to it in the input folder.
        /// </summary>
        /// <param name="fileName">The name of the file to create. This value should not contain path separators or be null or empty.</param>
        /// <param name="content">The text content to write to the newly created file.</param>
        /// <returns>The full path to the created file.</returns>
        public string CreateInputFile(string fileName, string content)
        {
            string path = Path.Combine(InputFolder, fileName);
            File.WriteAllText(path, content);
            return path;
        }

        /// <summary>
        /// Creates a new file with the specified name and writes the provided content to it in the output folder.
        /// </summary>
        /// <remarks>If a file with the same name already exists in the output folder, it will be
        /// overwritten.</remarks>
        /// <param name="fileName">The name of the file to create, including the file extension. Cannot be null or empty.</param>
        /// <param name="content">The text content to write to the newly created file.</param>
        /// <returns>The full path to the created file.</returns>
        public string CreateOutputFile(string fileName, string content)
        {
            string path = Path.Combine(OutputFolder, fileName);
            File.WriteAllText(path, content);
            return path;
        }
    }
}
