namespace ImageRenamr.Core.Models;

/// <summary>
/// Identifies the structured state represented by a rename progress update.
/// </summary>
public enum RenameProgressStatus
{
    /// <summary>The input folder is being scanned.</summary>
    Scanning,

    /// <summary>A file was copied with its generated name.</summary>
    Renamed,

    /// <summary>An existing target was overwritten.</summary>
    Overwritten,

    /// <summary>A file was skipped because its target already exists.</summary>
    Skipped,

    /// <summary>A file could not be processed.</summary>
    Failed,

    /// <summary>The scan did not find supported image files.</summary>
    NoFiles
}
