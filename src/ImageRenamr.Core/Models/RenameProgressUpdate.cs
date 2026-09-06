namespace ImageRenamr.Core.Models;

/// <summary>
/// Represents a structured progress update for a batch image renaming operation.
/// </summary>
/// <remarks>
/// This record is typically used to report progress updates to
/// the user interface or logging systems during long-running batch
/// rename operations.
/// </remarks>
/// <param name="ProcessedCount">The number of image files that
/// have been processed so far.</param>
/// <param name="TotalCount">The total number of image files to be
/// processed in the operation.</param>
/// <param name="Status">The outcome represented by this update.</param>
/// <param name="OriginalFileName">The source file name, when the update concerns a file.</param>
/// <param name="NewFileName">The generated target file name, when available.</param>
/// <param name="FailureReason">A structured reason for a skipped or failed item.</param>
public sealed record RenameProgressUpdate(
    int ProcessedCount,
    int TotalCount,
    RenameProgressStatus Status,
    string? OriginalFileName = null,
    string? NewFileName = null,
    RenameFailureReason FailureReason = RenameFailureReason.None);
