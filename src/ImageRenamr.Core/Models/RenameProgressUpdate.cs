namespace ImageRenamr.Core.Models;

/// <summary>
/// Represents a progress update for a batch image renaming operation,
/// including the number of files processed, the total number of files,
/// and a status message.
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
/// <param name="Message">A status message describing the
/// current progress or state of the renaming operation.</param>
public sealed record RenameProgressUpdate(
    int ProcessedCount,
    int TotalCount,
    string Message);
