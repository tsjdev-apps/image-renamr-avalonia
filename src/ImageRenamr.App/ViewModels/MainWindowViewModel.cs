using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.ExceptionServices;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRenamr.App.Resources.Localization;
using ImageRenamr.Core.Models;
using ImageRenamr.Core.Services;

namespace ImageRenamr.App.ViewModels;

/// <summary>
/// Provides the editable rename configuration and observable progress state for the main window.
/// </summary>
public sealed partial class MainWindowViewModel : ObservableObject
{
    private readonly IImageRenamrService imageRenamrService;

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    public MainWindowViewModel(IImageRenamrService imageRenamrService)
    {
        this.imageRenamrService = imageRenamrService;
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(CanEditInputs))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string InputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string OutputFolder { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string Prefix { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool OverwriteExisting { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressValue { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressBarMaximum))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentage))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressMaximum { get; set; }

    /// <summary>Gets a nonzero maximum suitable for the progress bar before scanning completes.</summary>
    public int ProgressBarMaximum => Math.Max(1, ProgressMaximum);

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = Strings.Status_Ready;

    [ObservableProperty]
    public partial string SummaryMessage { get; set; } = Strings.Progress_NoFiles;

    /// <summary>Gets the entries produced by the current rename operation.</summary>
    public ObservableCollection<RenameEntry> RenameHistory { get; } = [];

    /// <summary>Gets a value indicating whether configuration controls can be edited.</summary>
    public bool CanEditInputs => !IsBusy;

    /// <summary>Gets a value indicating whether a validation message is present.</summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>Gets a value indicating whether the rename history is empty.</summary>
    public bool IsHistoryEmpty => RenameHistory.Count == 0;

    /// <summary>Gets a value indicating whether the rename history contains entries.</summary>
    public bool HasRenameHistory => RenameHistory.Count > 0;

    /// <summary>Gets the progress as a percentage between zero and one hundred.</summary>
    public int ProgressPercentage => ProgressMaximum <= 0
        ? 0
        : (int)Math.Round(
            (double)ProgressValue / ProgressMaximum * 100,
            MidpointRounding.AwayFromZero);

    /// <summary>Gets the localized progress percentage.</summary>
    public string ProgressPercentageText =>
        $"{ProgressPercentage.ToString(CultureInfo.CurrentCulture)}%";

    /// <summary>Gets the localized processed-file count.</summary>
    public string ProgressSummary => Strings.FormatProgress(ProgressValue, ProgressMaximum);

    /// <summary>Gets the localized start-button text for the current state.</summary>
    public string StartButtonText => IsBusy
        ? Strings.RenamingImages_Button
        : Strings.RenameImages_Button;

    /// <summary>Gets or sets the view-provided source-folder picker.</summary>
    public Func<Task<string?>>? PickInputFolderDelegate { get; set; }

    /// <summary>Gets or sets the view-provided output-folder picker.</summary>
    public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        ResetProgress();
        IsBusy = true;
        StatusMessage = Strings.Status_Preparing;
        SummaryMessage = Strings.Summary_Validating;

        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;
        IProgress<RenameProgressUpdate> progress =
            new ContextProgress<RenameProgressUpdate>(synchronizationContext, ApplyProgressUpdate);

        try
        {
            RenameImagesRequest request = new(InputFolder, OutputFolder, Prefix, OverwriteExisting);
            RenameImagesResult result = await Task.Run(
                () => imageRenamrService.RenameAsync(request, progress));

            ProgressMaximum = result.TotalFiles;
            ProgressValue = result.TotalFiles;

            if (result.TotalFiles == 0)
            {
                SummaryMessage = Strings.Summary_NoImages;
                StatusMessage = Strings.Status_NoImages;
                AddHistoryEntry(new RenameEntry { Status = RenameEntryStatus.CompletedNoFiles });
                return;
            }

            SummaryMessage = Strings.FormatSummary(result.CopiedFiles, result.SkippedFiles);
            StatusMessage = Strings.Status_Completed;
            AddHistoryEntry(
                new RenameEntry
                {
                    Status = RenameEntryStatus.Completed,
                    CopiedFiles = result.CopiedFiles,
                    SkippedFiles = result.SkippedFiles
                });
        }
        catch (ArgumentException exception)
        {
            string validationMessage = GetArgumentValidationMessage(exception);
            HandleExpectedFailure(Strings.Validation_ReviewInput, validationMessage);
        }
        catch (DirectoryNotFoundException)
        {
            HandleExpectedFailure(Strings.Validation_InputMissing, Strings.Validation_InputMissing);
        }
        catch (IOException)
        {
            HandleExpectedFailure(Strings.Validation_FileSystem, Strings.Validation_FileSystem);
        }
        catch (UnauthorizedAccessException)
        {
            HandleExpectedFailure(Strings.Validation_AccessDenied, Strings.Validation_AccessDenied);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickInputFolderAsync()
    {
        if (PickInputFolderDelegate is null)
        {
            throw new InvalidOperationException(
                "No folder-pick delegate assigned. The view must assign PickInputFolderDelegate.");
        }

        InputFolder = await PickInputFolderDelegate() ?? string.Empty;
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickOutputFolderAsync()
    {
        if (PickOutputFolderDelegate is null)
        {
            throw new InvalidOperationException(
                "No folder-pick delegate assigned. The view must assign PickOutputFolderDelegate.");
        }

        OutputFolder = await PickOutputFolderDelegate() ?? string.Empty;
    }

    private bool CanStart()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(InputFolder)
               && !string.IsNullOrWhiteSpace(OutputFolder)
               && !string.IsNullOrWhiteSpace(Prefix);
    }

    private void ResetProgress()
    {
        ValidationMessage = string.Empty;
        ProgressValue = 0;
        ProgressMaximum = 0;
        RenameHistory.Clear();
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(HasRenameHistory));
    }

    private void ApplyProgressUpdate(RenameProgressUpdate update)
    {
        ProgressMaximum = Math.Max(0, update.TotalCount);
        ProgressValue = Math.Clamp(update.ProcessedCount, 0, ProgressMaximum);

        if (update.Status == RenameProgressStatus.Scanning)
        {
            StatusMessage = Strings.Status_Scanning;
            return;
        }

        if (update.Status == RenameProgressStatus.NoFiles)
        {
            StatusMessage = Strings.Status_NoImages;
            return;
        }

        AddHistoryEntry(RenameEntry.FromProgress(update));
    }

    private void AddHistoryEntry(RenameEntry entry)
    {
        RenameHistory.Add(entry);
        OnPropertyChanged(nameof(IsHistoryEmpty));
        OnPropertyChanged(nameof(HasRenameHistory));
    }

    private void HandleExpectedFailure(string status, string validationMessage)
    {
        StatusMessage = status;
        SummaryMessage = Strings.Summary_BatchFailed;
        ValidationMessage = validationMessage;
        AddHistoryEntry(new RenameEntry { Status = RenameEntryStatus.BatchFailed });
    }

    private string GetArgumentValidationMessage(ArgumentException exception)
    {
        if (exception.ParamName == nameof(RenameImagesRequest.Prefix))
        {
            return string.IsNullOrWhiteSpace(Prefix)
                ? Strings.Validation_PrefixRequired
                : Strings.Validation_PrefixInvalid;
        }

        if (exception.ParamName is nameof(RenameImagesRequest.InputFolder)
            or nameof(RenameImagesRequest.OutputFolder))
        {
            return Strings.Validation_FoldersRequired;
        }

        return Strings.Validation_FoldersMustDiffer;
    }

    private sealed class ContextProgress<T>(
        SynchronizationContext? synchronizationContext,
        Action<T> handler) : IProgress<T>
    {
        [SuppressMessage(
            "Design",
            "CA1031:Do not catch general exception types",
            Justification = "The exception is captured on the UI context and rethrown unchanged on the reporting thread.")]
        public void Report(T value)
        {
            if (synchronizationContext is null
                || ReferenceEquals(SynchronizationContext.Current, synchronizationContext))
            {
                handler(value);
                return;
            }

            Exception? callbackException = null;
            using ManualResetEventSlim completed = new(initialState: false);

            synchronizationContext.Post(
                _ =>
                {
                    try
                    {
                        handler(value);
                    }
                    catch (Exception exception)
                    {
                        callbackException = exception;
                    }
                    finally
                    {
                        completed.Set();
                    }
                },
                state: null);

            completed.Wait();

            if (callbackException is not null)
            {
                ExceptionDispatchInfo.Capture(callbackException).Throw();
            }
        }
    }
}
