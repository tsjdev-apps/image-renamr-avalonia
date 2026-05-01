using ImageRenamr.App.ViewModels;
using ImageRenamr.Core.Models;
using ImageRenamr.Core.Services;

namespace ImageRenamr.Tests;

/// <summary>
/// Contains unit tests for the MainWindowViewModel class.
/// </summary>
public class MainWindowViewModelTests
{
    /// <summary>
    /// Verifies that folder picker commands store the selected paths in the bound properties.
    /// </summary>
    [Fact]
    public async Task FolderPickerCommandsUpdateSelectedPaths()
    {
        MainWindowViewModel viewModel = new(new StubImageRenamrService())
        {
            PickInputFolderDelegate = () => Task.FromResult<string?>("C:\\Images\\Source"),
            PickOutputFolderDelegate = () => Task.FromResult<string?>("C:\\Images\\Renamed")
        };

        await viewModel.PickInputFolderCommand.ExecuteAsync(null);
        await viewModel.PickOutputFolderCommand.ExecuteAsync(null);

        Assert.Equal("C:\\Images\\Source", viewModel.InputFolder);
        Assert.Equal("C:\\Images\\Renamed", viewModel.OutputFolder);
    }

    /// <summary>
    /// Verifies that selected paths are shown directly and the start command still requires a prefix.
    /// </summary>
    [Fact]
    public void StartCommandRequiresInputFolderOutputFolderAndPrefix()
    {
        MainWindowViewModel viewModel = new(new StubImageRenamrService())
        {
            InputFolder = "C:\\Images\\Source",
            OutputFolder = "C:\\Images\\Renamed"
        };

        Assert.False(viewModel.StartCommand.CanExecute(null));

        viewModel.Prefix = "TRIP";

        Assert.True(viewModel.StartCommand.CanExecute(null));
    }

    /// <summary>
    /// Verifies that a successful rename updates the user-facing progress and summary state.
    /// </summary>
    /// <returns>A task that represents the asynchronous test operation.</returns>
    [Fact]
    public async Task StartCommandUpdatesProgressAndSummaryAfterSuccessfulRename()
    {
        StubImageRenamrService service = new()
        {
            Handler = (request, _, _) =>
            {
                return Task.FromResult(
                    new RenameImagesResult(
                        TotalFiles: 2,
                        CopiedFiles: 1,
                        SkippedFiles: 1,
                        OutputFiles: [Path.Combine(request.OutputFolder, "TRIP_01.jpg")]));
            }
        };

        MainWindowViewModel viewModel = new(service)
        {
            InputFolder = "source",
            OutputFolder = "destination",
            Prefix = "TRIP",
            OverwriteExisting = true
        };

        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.NotNull(service.LastRequest);
        Assert.Equal("source", service.LastRequest.InputFolder);
        Assert.Equal("destination", service.LastRequest.OutputFolder);
        Assert.Equal("TRIP", service.LastRequest.Prefix);
        Assert.True(service.LastRequest.OverwriteExisting);
        Assert.False(viewModel.IsBusy);
        Assert.Equal(2, viewModel.ProgressMaximum);
        Assert.Equal(2, viewModel.ProgressValue);
        Assert.Equal("100%", viewModel.ProgressPercentageText);
        Assert.Equal("1 copied and 1 skipped.", viewModel.SummaryMessage);
        Assert.Equal("1 copied and 1 skipped.", viewModel.LatestActivityMessage);
        Assert.Equal("Rename operation completed successfully.", viewModel.StatusMessage);
    }

    private sealed class StubImageRenamrService : IImageRenamrService
    {
        public RenameImagesRequest? LastRequest { get; private set; }

        public Func<RenameImagesRequest, IProgress<RenameProgressUpdate>?, CancellationToken, Task<RenameImagesResult>> Handler { get; init; } =
            (request, _, _) => Task.FromResult(new RenameImagesResult(0, 0, 0, []));

        public Task<RenameImagesResult> RenameAsync(
            RenameImagesRequest request,
            IProgress<RenameProgressUpdate>? progress = null,
            CancellationToken cancellationToken = default)
        {
            LastRequest = request;
            return Handler(request, progress, cancellationToken);
        }
    }
}
