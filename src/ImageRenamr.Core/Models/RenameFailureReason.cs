namespace ImageRenamr.Core.Models;

/// <summary>
/// Identifies a user-presentable reason for a skipped or failed rename operation.
/// </summary>
public enum RenameFailureReason
{
    /// <summary>No failure occurred.</summary>
    None,

    /// <summary>The generated target file already exists.</summary>
    TargetExists,

    /// <summary>A file-system operation failed.</summary>
    FileSystemError,

    /// <summary>Access to a source or target path was denied.</summary>
    AccessDenied
}
