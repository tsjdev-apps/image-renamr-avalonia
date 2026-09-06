using ImageRenamr.App.Resources.Localization;
using ImageRenamr.Core.Models;

namespace ImageRenamr.App.ViewModels;

/// <summary>
/// Represents one compact, localized entry in the rename history.
/// </summary>
public sealed class RenameEntry
{
    /// <summary>Gets the source file name for a file entry.</summary>
    public string? OriginalFileName { get; init; }

    /// <summary>Gets the generated target file name for a file entry.</summary>
    public string? NewFileName { get; init; }

    /// <summary>Gets the structured outcome of the entry.</summary>
    public RenameEntryStatus Status { get; init; }

    /// <summary>Gets the number of successfully copied files for a summary entry.</summary>
    public int CopiedFiles { get; init; }

    /// <summary>Gets the number of skipped files for a summary entry.</summary>
    public int SkippedFiles { get; init; }

    /// <summary>Gets a symbol that communicates the outcome without relying on color.</summary>
    public string StatusIcon => Status switch
    {
        RenameEntryStatus.Skipped or RenameEntryStatus.CompletedNoFiles => "–",
        RenameEntryStatus.Overwritten => "↻",
        RenameEntryStatus.Completed => "Σ",
        RenameEntryStatus.FailedFileSystem
            or RenameEntryStatus.FailedAccessDenied
            or RenameEntryStatus.BatchFailed => "!",
        _ => "✓"
    };

    /// <summary>Gets the localized primary line.</summary>
    public string PrimaryText => Status switch
    {
        RenameEntryStatus.Completed => Strings.RenameHistory_CompletedTitle,
        RenameEntryStatus.CompletedNoFiles => Strings.RenameHistory_NoFilesTitle,
        RenameEntryStatus.BatchFailed => Strings.RenameHistory_FailedTitle,
        _ => OriginalFileName ?? string.Empty
    };

    /// <summary>Gets the localized secondary line.</summary>
    public string SecondaryText => Status switch
    {
        RenameEntryStatus.Renamed or RenameEntryStatus.Overwritten =>
            Strings.FormatNewName(NewFileName ?? string.Empty),
        RenameEntryStatus.Skipped => Strings.FormatTargetExists(NewFileName ?? string.Empty),
        RenameEntryStatus.FailedAccessDenied => Strings.RenameError_AccessDenied,
        RenameEntryStatus.FailedFileSystem => Strings.RenameError_FileSystem,
        RenameEntryStatus.Completed => Strings.FormatSummary(CopiedFiles, SkippedFiles),
        RenameEntryStatus.CompletedNoFiles => Strings.Summary_NoImages,
        RenameEntryStatus.BatchFailed => Strings.Summary_BatchFailed,
        _ => string.Empty
    };

    /// <summary>Gets the localized status label.</summary>
    public string StatusText => Status switch
    {
        RenameEntryStatus.Renamed => Strings.RenameStatus_Renamed,
        RenameEntryStatus.Overwritten => Strings.RenameStatus_Overwritten,
        RenameEntryStatus.Skipped => Strings.RenameStatus_Skipped,
        RenameEntryStatus.FailedFileSystem
            or RenameEntryStatus.FailedAccessDenied
            or RenameEntryStatus.BatchFailed => Strings.RenameStatus_Failed,
        _ => Strings.RenameStatus_Completed
    };

    /// <summary>Gets the file name or summary title displayed on the first line.</summary>
    public string FileName => PrimaryText;

    /// <summary>Gets the generated file name displayed as the destination.</summary>
    public string? Destination => Status is RenameEntryStatus.Renamed
        or RenameEntryStatus.Overwritten
        or RenameEntryStatus.Skipped
        or RenameEntryStatus.FailedFileSystem
        or RenameEntryStatus.FailedAccessDenied
            ? NewFileName
            : null;

    /// <summary>Gets localized details for skipped, failed, and summary entries.</summary>
    public string? Details => Status switch
    {
        RenameEntryStatus.Skipped => Strings.RenameError_TargetExistsShort,
        RenameEntryStatus.FailedAccessDenied => Strings.RenameError_AccessDenied,
        RenameEntryStatus.FailedFileSystem => Strings.RenameError_FileSystem,
        RenameEntryStatus.Completed => Strings.FormatSummary(CopiedFiles, SkippedFiles),
        RenameEntryStatus.CompletedNoFiles => Strings.Summary_NoImages,
        RenameEntryStatus.BatchFailed => Strings.Summary_BatchFailed,
        _ => null
    };

    /// <summary>Gets a value indicating whether a destination should be shown.</summary>
    public bool HasDestination => !string.IsNullOrWhiteSpace(Destination);

    /// <summary>Gets a value indicating whether details should be shown.</summary>
    public bool HasDetails => !string.IsNullOrWhiteSpace(Details);

    /// <summary>Gets a value indicating whether this entry summarizes the batch.</summary>
    public bool IsSummary => Status is RenameEntryStatus.Completed
        or RenameEntryStatus.CompletedNoFiles
        or RenameEntryStatus.BatchFailed;

    /// <summary>Creates a file entry from a structured service progress update.</summary>
    public static RenameEntry FromProgress(RenameProgressUpdate update)
    {
        ArgumentNullException.ThrowIfNull(update);

        return new RenameEntry
        {
            OriginalFileName = update.OriginalFileName,
            NewFileName = update.NewFileName,
            Status = update.Status switch
            {
                RenameProgressStatus.Renamed => RenameEntryStatus.Renamed,
                RenameProgressStatus.Overwritten => RenameEntryStatus.Overwritten,
                RenameProgressStatus.Skipped => RenameEntryStatus.Skipped,
                RenameProgressStatus.Failed when update.FailureReason == RenameFailureReason.AccessDenied =>
                    RenameEntryStatus.FailedAccessDenied,
                RenameProgressStatus.Failed => RenameEntryStatus.FailedFileSystem,
                _ => throw new ArgumentOutOfRangeException(nameof(update))
            }
        };
    }
}

/// <summary>
/// Identifies the structured outcome displayed by a rename history entry.
/// </summary>
public enum RenameEntryStatus
{
    Renamed,
    Overwritten,
    Skipped,
    FailedFileSystem,
    FailedAccessDenied,
    Completed,
    CompletedNoFiles,
    BatchFailed
}
