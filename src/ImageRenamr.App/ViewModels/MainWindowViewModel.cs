using System.Diagnostics;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ImageRenamr.Core.Models;
using ImageRenamr.Core.Services;

namespace ImageRenamr.App.ViewModels;

public sealed partial class MainWindowViewModel(
    IImageRenamrService imageRenamrService) : ObservableObject
{
    private const int LatestActivityUpdateIntervalMilliseconds = 250;

    /// <summary>
    /// Gets or sets a value indicating whether a background operation is currently in progress.
    /// </summary>
    /// <remarks>When set to <see langword="true"/>, certain UI elements may be disabled to prevent user
    /// interaction during processing. This property is typically used to provide feedback to the user and to control
    /// the availability of commands or inputs while an operation is running.</remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    [NotifyPropertyChangedFor(nameof(CanEditInputs))]
    [NotifyPropertyChangedFor(nameof(StartButtonText))]
    public partial bool IsBusy { get; set; }

    /// <summary>
    /// Gets or sets the path to the folder containing the image files to be renamed.
    /// </summary>
    /// <remarks>The specified folder should exist and contain supported image formats such as JPEG, PNG, GIF,
    /// BMP, or WebP. Changing this property may affect the availability of related commands or operations.</remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string InputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the path to the folder where renamed image files will be saved.
    /// </summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string OutputFolder { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the prefix to apply to renamed image files.
    /// </summary>
    /// <remarks>The specified prefix is added to the beginning of each file name during the renaming
    /// operation. Changing this property may affect the availability of related commands if command execution depends
    /// on the prefix value.</remarks>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartCommand))]
    public partial string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether existing files should be overwritten during the renaming process.
    /// </summary>
    /// <remarks>Set this property to <see langword="true"/> to allow the application to replace files with
    /// the same name in the target directory. If set to <see langword="false"/>, the application will skip renaming
    /// files that would overwrite existing ones.</remarks>
    [ObservableProperty]
    public partial bool OverwriteExisting { get; set; }

    /// <summary>
    /// Gets or sets the validation message associated with the current state.
    /// </summary>
    /// <remarks>This property is typically used to display user-facing validation errors or informational
    /// messages in the UI. The value is updated when validation logic detects an issue or when the state
    /// changes.</remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasValidationMessage))]
    public partial string ValidationMessage { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current progress value of the operation.
    /// </summary>
    /// <remarks>This property typically represents the number of completed steps or items processed in a
    /// multi-step operation. Changes to this property may update related progress display properties.</remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressValue { get; set; }

    /// <summary>
    /// Gets or sets the maximum value for the progress indicator.
    /// </summary>
    /// <remarks>Set this property to define the upper bound of the progress range. This value is typically
    /// used to represent the total number of steps or items to process in a progress-tracking scenario.</remarks>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ProgressSummary))]
    [NotifyPropertyChangedFor(nameof(ProgressPercentageText))]
    public partial int ProgressMaximum { get; set; } = 1;

    /// <summary>
    /// Gets or sets the current status message displayed to the user.
    /// </summary>
    /// <remarks>This property is typically used to provide feedback or instructions during the image renaming
    /// process. The value may be updated to reflect progress, errors, or completion states.</remarks>
    [ObservableProperty]
    public partial string StatusMessage { get; set; } = "Choose your folders, enter a prefix, and start the batch.";

    /// <summary>
    /// Gets or sets the summary message that describes the current processing status.
    /// </summary>
    [ObservableProperty]
    public partial string SummaryMessage { get; set; } = "No files have been processed yet.";

    /// <summary>
    /// Gets or sets the latest detailed activity message shown in the live progress card.
    /// </summary>
    [ObservableProperty]
    public partial string LatestActivityMessage { get; set; } = "No rename activity yet.";

    /// <summary>
    /// Gets a value indicating whether input fields can be edited.
    /// </summary>
    /// <remarks>Input fields are editable only when the operation is not in progress. This property is
    /// typically used to enable or disable UI elements based on the application's busy state.</remarks>
    public bool CanEditInputs => !IsBusy;

    /// <summary>
    /// Gets a value indicating whether a validation message is present.
    /// </summary>
    public bool HasValidationMessage => !string.IsNullOrWhiteSpace(ValidationMessage);

    /// <summary>
    /// Gets the progress percentage as a formatted string suitable for display.
    /// </summary>
    /// <remarks>The returned string represents the current progress as a percentage, rounded to the nearest
    /// whole number and followed by a percent sign. If the maximum progress value is zero or less, the result is
    /// "0%".</remarks>
    public string ProgressPercentageText => ProgressMaximum <= 0
        ? "0%"
        : $"{Math.Round((double)ProgressValue / ProgressMaximum * 100,
            MidpointRounding.AwayFromZero).ToString(CultureInfo.InvariantCulture)}%";

    /// <summary>
    /// Gets a summary of the current progress, indicating the number of files processed out of the total.
    /// </summary>
    public string ProgressSummary => $"{ProgressValue} / {ProgressMaximum} files processed";

    /// <summary>
    /// Gets the text displayed on the start button, reflecting the current operation state.
    /// </summary>
    /// <remarks>The button text indicates whether the application is currently performing the renaming
    /// operation or is ready to start. When a renaming operation is in progress, the text changes to inform the
    /// user.</remarks>
    public string StartButtonText => IsBusy ? "Renaming images..." : "Rename images";

    /// <summary>
    /// Gets or sets the delegate used to display a folder picker dialog for selecting the input folder.
    /// </summary>
    /// <remarks>Assign this property to provide a custom implementation for folder selection, such as
    /// invoking a platform-specific dialog. The delegate should return the selected folder path as a string, or null if
    /// the operation is canceled.</remarks>
    public Func<Task<string?>>? PickInputFolderDelegate { get; set; }

    /// <summary>
    /// Gets or sets the delegate used to asynchronously prompt the user to select an output folder.
    /// </summary>
    /// <remarks>The delegate should return a task that completes with the selected folder path as a string,
    /// or null if the user cancels the operation. This property enables decoupling of UI-specific folder selection
    /// logic from the core application logic.</remarks>
    public Func<Task<string?>>? PickOutputFolderDelegate { get; set; }

    /// <summary>
    /// Initiates the image renaming operation using the current input, output, and prefix settings.
    /// </summary>
    /// <remarks>This command validates the input and output folders, applies the specified prefix, and
    /// performs the renaming process for supported image files. Progress and status updates are provided throughout the
    /// operation. The command cannot be executed concurrently.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    [RelayCommand(AllowConcurrentExecutions = false, CanExecute = nameof(CanStart))]
    private async Task StartAsync()
    {
        ValidationMessage = string.Empty;
        IsBusy = true;
        ProgressValue = 0;
        ProgressMaximum = 1;
        StatusMessage = "Preparing the rename operation.";
        SummaryMessage = "Validating your folders and prefix.";
        LatestActivityMessage = "Scanning the selected folder for supported image files.";
        Stopwatch latestActivityStopwatch = Stopwatch.StartNew();
        int acceptProgressUpdates = 1;

        Progress<RenameProgressUpdate> progress = new(update =>
        {
            if (Volatile.Read(ref acceptProgressUpdates) == 0)
            {
                return;
            }

            ProgressMaximum = Math.Max(1, update.TotalCount);
            ProgressValue = Math.Min(update.ProcessedCount, ProgressMaximum);

            if (update.ProcessedCount == 0
                || update.ProcessedCount == update.TotalCount
                || latestActivityStopwatch.ElapsedMilliseconds >= LatestActivityUpdateIntervalMilliseconds)
            {
                LatestActivityMessage = update.Message;
                latestActivityStopwatch.Restart();
            }
        });

        try
        {
            RenameImagesRequest request = new(InputFolder, OutputFolder, Prefix, OverwriteExisting);
            RenameImagesResult result = await imageRenamrService.RenameAsync(request, progress);
            Volatile.Write(ref acceptProgressUpdates, 0);

            ProgressMaximum = Math.Max(1, result.TotalFiles);
            ProgressValue = result.TotalFiles;

            if (result.TotalFiles == 0)
            {
                SummaryMessage = "Nothing was copied because no supported images were found.";
                StatusMessage = "The input folder did not contain any supported image files.";
                LatestActivityMessage = SummaryMessage;
                return;
            }

            SummaryMessage =
                $"{result.CopiedFiles.ToString(CultureInfo.CurrentCulture)} copied " +
                $"and {result.SkippedFiles.ToString(CultureInfo.CurrentCulture)} skipped.";

            StatusMessage = "Rename operation completed successfully.";
            LatestActivityMessage = SummaryMessage;
        }
        catch (ArgumentException exception)
        {
            HandleExpectedFailure("Please review the folders and prefix, then try again.", exception.Message);
        }
        catch (DirectoryNotFoundException exception)
        {
            HandleExpectedFailure("The input folder could not be found.", exception.Message);
        }
        catch (IOException exception)
        {
            HandleExpectedFailure("A file-system error interrupted the rename operation.", exception.Message);
        }
        catch (UnauthorizedAccessException exception)
        {
            HandleExpectedFailure("The app does not have permission to access one of the selected folders.", exception.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Prompts the user to select an input folder and updates the input folder path accordingly.
    /// </summary>
    /// <remarks>This method relies on a delegate assigned by the view to present a folder picker dialog. If
    /// the user cancels the dialog, the input folder path is set to an empty string.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no folder-pick delegate is assigned.
    /// The view must assign PickInputFolderDelegate before calling this method.</exception>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickInputFolderAsync()
    {
        if (PickInputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickInputFolderDelegate.");
        }

        InputFolder = await PickInputFolderDelegate() ?? string.Empty;
    }

    /// <summary>
    /// Prompts the user to select an output folder and updates the OutputFolder property with the selected path.
    /// </summary>
    /// <remarks>This method relies on a delegate assigned by the view to perform the folder selection. The
    /// OutputFolder property is set to the selected folder path, or to an empty string if the user cancels the
    /// operation.</remarks>
    /// <returns>A task that represents the asynchronous operation.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no folder-pick delegate is assigned to PickOutputFolderDelegate.</exception>
    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task PickOutputFolderAsync()
    {
        if (PickOutputFolderDelegate is null)
        {
            throw new InvalidOperationException("No folder-pick delegate assigned. The view must assign PickOutputFolderDelegate.");
        }

        OutputFolder = await PickOutputFolderDelegate() ?? string.Empty;
    }

    /// <summary>
    /// Determines whether the start operation can be initiated based on the current state and required input values.
    /// </summary>
    /// <remarks>The operation can start only when the application is not busy and all required
    /// fields—InputFolder, OutputFolder, and Prefix—are specified and not empty.</remarks>
    /// <returns>true if the operation can start; otherwise, false.</returns>
    private bool CanStart()
    {
        return !IsBusy
               && !string.IsNullOrWhiteSpace(InputFolder)
               && !string.IsNullOrWhiteSpace(OutputFolder)
               && !string.IsNullOrWhiteSpace(Prefix);
    }

    /// <summary>
    /// Handles an expected failure by updating the status, summary, and validation messages to reflect the failure
    /// state.
    /// </summary>
    /// <param name="status">The status message to display, describing the nature of the failure.</param>
    /// <param name="details">Additional details providing context or explanation for the failure.</param>
    private void HandleExpectedFailure(
        string status,
        string details)
    {
        StatusMessage = status;
        SummaryMessage = "The batch could not be completed.";
        LatestActivityMessage = details;
        ValidationMessage = details;
    }
}
