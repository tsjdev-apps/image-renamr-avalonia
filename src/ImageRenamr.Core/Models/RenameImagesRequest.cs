namespace ImageRenamr.Core.Models;

/// <summary>
/// Represents the parameters required to
/// perform a batch image renaming operation.
/// </summary>
/// <remarks>
/// Use this record to specify all necessary
/// options for a single image renaming operation, including
/// source and destination folders, file naming, and overwrite behavior.
/// </remarks>
/// <param name="InputFolder">The full path to the folder
/// containing the image files to be renamed. Cannot be null or empty.</param>
/// <param name="OutputFolder">The full path to the destination folder
/// where renamed images will be saved. Cannot be null or empty.</param>
/// <param name="Prefix">The prefix to apply to each renamed
/// image file. Cannot be null, empty, or whitespace.</param>
/// <param name="OverwriteExisting">true to overwrite existing files in the
/// output folder with the same name; otherwise, false.</param>
public sealed record RenameImagesRequest(
    string InputFolder,
    string OutputFolder,
    string Prefix,
    bool OverwriteExisting);
