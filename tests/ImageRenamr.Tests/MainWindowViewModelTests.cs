using ImageRenamr.App.Resources.Localization;
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
            Handler = (request, progress, _) =>
            {
                progress?.Report(
                    new RenameProgressUpdate(
                        1,
                        2,
                        RenameProgressStatus.Renamed,
                        "camera.jpg",
                        "TRIP_01.jpg"));
                progress?.Report(
                    new RenameProgressUpdate(
                        2,
                        2,
                        RenameProgressStatus.Skipped,
                        "portrait.png",
                        "TRIP_02.png",
                        RenameFailureReason.TargetExists));

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
        Assert.Equal(Strings.FormatSummary(1, 1), viewModel.SummaryMessage);
        Assert.Equal(Strings.Status_Completed, viewModel.StatusMessage);
        Assert.Collection(
            viewModel.RenameHistory,
            entry =>
            {
                Assert.Equal(RenameEntryStatus.Renamed, entry.Status);
                Assert.Equal("camera.jpg", entry.OriginalFileName);
                Assert.Equal("TRIP_01.jpg", entry.NewFileName);
            },
            entry => Assert.Equal(RenameEntryStatus.Skipped, entry.Status),
            entry =>
            {
                Assert.Equal(RenameEntryStatus.Completed, entry.Status);
                Assert.True(entry.IsSummary);
            });
    }

    /// <summary>
    /// Verifies that the view model offloads the rename operation from the caller thread
    /// while still applying progress updates through the captured synchronization context.
    /// </summary>
    [Fact]
    public void StartCommandOffloadsRenameWorkAndMarshalsProgressUpdates()
    {
        using PumpingSynchronizationContext synchronizationContext = new();
        int callerThreadId = 0;
        int serviceThreadId = 0;
        int progressUpdateThreadId = 0;

        synchronizationContext.Run(async () =>
        {
            callerThreadId = Environment.CurrentManagedThreadId;

            StubImageRenamrService service = new()
            {
                Handler = (request, progress, _) =>
                {
                    serviceThreadId = Environment.CurrentManagedThreadId;
                    progress?.Report(
                        new RenameProgressUpdate(
                            1,
                            1,
                            RenameProgressStatus.Renamed,
                            "source.jpg",
                            $"{request.Prefix}_01.jpg"));

                    return Task.FromResult(
                        new RenameImagesResult(
                            TotalFiles: 1,
                            CopiedFiles: 1,
                            SkippedFiles: 0,
                            OutputFiles: [Path.Combine(request.OutputFolder, $"{request.Prefix}_01.jpg")]));
                }
            };

            MainWindowViewModel viewModel = new(service)
            {
                InputFolder = "source",
                OutputFolder = "destination",
                Prefix = "TRIP"
            };

            viewModel.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(MainWindowViewModel.ProgressValue)
                    && viewModel.ProgressValue > 0)
                {
                    progressUpdateThreadId = Environment.CurrentManagedThreadId;
                }
            };

            await viewModel.StartCommand.ExecuteAsync(null);

            Assert.False(viewModel.IsBusy);
            Assert.Equal(1, viewModel.ProgressValue);
            Assert.Equal("100%", viewModel.ProgressPercentageText);
            Assert.Equal(Strings.FormatSummary(1, 0), viewModel.SummaryMessage);
            Assert.Equal(Strings.Status_Completed, viewModel.StatusMessage);
            Assert.Equal(2, viewModel.RenameHistory.Count);
        });

        Assert.NotEqual(callerThreadId, serviceThreadId);
        Assert.Equal(callerThreadId, progressUpdateThreadId);
    }

    /// <summary>
    /// Verifies that starting a new operation removes all entries from the previous operation.
    /// </summary>
    [Fact]
    public async Task StartCommandClearsRenameHistoryBeforeEachOperation()
    {
        int operationNumber = 0;
        StubImageRenamrService service = new()
        {
            Handler = (_, progress, _) =>
            {
                operationNumber++;
                progress?.Report(
                    new RenameProgressUpdate(
                        1,
                        1,
                        RenameProgressStatus.Renamed,
                        $"source-{operationNumber}.jpg",
                        $"target-{operationNumber}.jpg"));

                return Task.FromResult(new RenameImagesResult(1, 1, 0, []));
            }
        };

        MainWindowViewModel viewModel = CreateRunnableViewModel(service);

        await viewModel.StartCommand.ExecuteAsync(null);
        await viewModel.StartCommand.ExecuteAsync(null);

        Assert.Equal(2, viewModel.RenameHistory.Count);
        Assert.Equal("source-2.jpg", viewModel.RenameHistory[0].OriginalFileName);
        Assert.Equal(RenameEntryStatus.Completed, viewModel.RenameHistory[1].Status);
    }

    /// <summary>
    /// Verifies that an empty batch produces a final summary entry.
    /// </summary>
    [Fact]
    public async Task StartCommandAddsFinalSummaryWhenNoImagesAreFound()
    {
        MainWindowViewModel viewModel = CreateRunnableViewModel(new StubImageRenamrService());

        await viewModel.StartCommand.ExecuteAsync(null);

        RenameEntry entry = Assert.Single(viewModel.RenameHistory);
        Assert.Equal(RenameEntryStatus.CompletedNoFiles, entry.Status);
        Assert.True(entry.IsSummary);
        Assert.Equal(0, viewModel.ProgressMaximum);
        Assert.Equal("0%", viewModel.ProgressPercentageText);
    }

    /// <summary>
    /// Verifies percentage rounding and the structured failed-file presentation.
    /// </summary>
    [Fact]
    public void ProgressAndFailedEntriesUseStructuredState()
    {
        MainWindowViewModel viewModel = new(new StubImageRenamrService())
        {
            ProgressValue = 58,
            ProgressMaximum = 150
        };

        RenameEntry failedEntry = RenameEntry.FromProgress(
            new RenameProgressUpdate(
                59,
                150,
                RenameProgressStatus.Failed,
                "locked.jpg",
                "TRIP_059.jpg",
                RenameFailureReason.AccessDenied));

        Assert.Equal(39, viewModel.ProgressPercentage);
        Assert.Equal("39%", viewModel.ProgressPercentageText);
        Assert.Equal(RenameEntryStatus.FailedAccessDenied, failedEntry.Status);
        Assert.Equal("!", failedEntry.StatusIcon);
        Assert.Equal(Strings.RenameStatus_Failed, failedEntry.StatusText);
        Assert.Equal(Strings.RenameError_AccessDenied, failedEntry.SecondaryText);
    }

    private static MainWindowViewModel CreateRunnableViewModel(IImageRenamrService service)
    {
        return new MainWindowViewModel(service)
        {
            InputFolder = "source",
            OutputFolder = "destination",
            Prefix = "TRIP"
        };
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

    private sealed class PumpingSynchronizationContext : SynchronizationContext, IDisposable
    {
        private readonly AutoResetEvent workItemsWaiting = new(initialState: false);
        private readonly Queue<(SendOrPostCallback Callback, object? State)> workItems = [];
        private bool completed;

        public void Dispose()
        {
            workItemsWaiting.Dispose();
        }

        public override void Post(SendOrPostCallback callback, object? state)
        {
            lock (workItems)
            {
                workItems.Enqueue((callback, state));
            }

            workItemsWaiting.Set();
        }

        public void Run(Func<Task> action)
        {
            SynchronizationContext? previousContext = Current;
            SetSynchronizationContext(this);

            try
            {
                Task task = action();

                _ = task.ContinueWith(
                    _ =>
                    {
                        completed = true;
                        workItemsWaiting.Set();
                    },
                    CancellationToken.None,
                    TaskContinuationOptions.None,
                    TaskScheduler.Default);

                while (!completed || HasPendingWork())
                {
                    if (TryDequeue(out (SendOrPostCallback Callback, object? State) workItem))
                    {
                        workItem.Callback(workItem.State);
                        continue;
                    }

                    workItemsWaiting.WaitOne();
                }

                task.GetAwaiter().GetResult();
            }
            finally
            {
                SetSynchronizationContext(previousContext);
            }
        }

        private bool HasPendingWork()
        {
            lock (workItems)
            {
                return workItems.Count > 0;
            }
        }

        private bool TryDequeue(out (SendOrPostCallback Callback, object? State) workItem)
        {
            lock (workItems)
            {
                if (workItems.Count > 0)
                {
                    workItem = workItems.Dequeue();
                    return true;
                }
            }

            workItem = default;
            return false;
        }
    }
}
