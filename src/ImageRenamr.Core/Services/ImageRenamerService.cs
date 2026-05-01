using System.Collections.Frozen;
using System.Globalization;
using ImageRenamr.Core.Models;

namespace ImageRenamr.Core.Services;

public sealed class ImageRenamerService : IImageRenamerService
{
    /// <summary>
    /// Represents the set of file extensions supported for image file operations.
    /// </summary>
    /// <remarks>
    /// The set includes common image formats such as
    /// AVIF, BMP, GIF, JPEG, PNG, TIFF, and WebP.
    /// Extension matching is case-insensitive.
    /// </remarks>
    private static readonly FrozenSet<string> SupportedExtensions = new[]
    {
        ".avif",
        ".bmp",
        ".gif",
        ".jpeg",
        ".jpg",
        ".png",
        ".tif",
        ".tiff",
        ".webp"
    }.ToFrozenSet(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc/>
    public async Task<RenameImagesResult> RenameAsync(
        RenameImagesRequest request,
        IProgress<RenameProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        string inputFolder =
            ValidateFolderPath(request.InputFolder, nameof(request.InputFolder));

        string outputFolder =
            ValidateFolderPath(request.OutputFolder, nameof(request.OutputFolder));

        string prefix =
            ValidatePrefix(request.Prefix);

        if (!Directory.Exists(inputFolder))
        {
            throw new DirectoryNotFoundException($"The input folder '{inputFolder}' does not exist.");
        }

        if (PathsMatch(inputFolder, outputFolder))
        {
            throw new ArgumentException(
                "Choose different input and output folders to keep the originals safe and avoid name collisions.");
        }

        _ = Directory.CreateDirectory(outputFolder);

        progress?.Report(new RenameProgressUpdate(0, 0, "Scanning the input folder for supported image files."));

        List<string> sourceFiles = [.. Directory
            .EnumerateFiles(inputFolder, "*", SearchOption.TopDirectoryOnly)
            .Where(filePath => SupportedExtensions.Contains(Path.GetExtension(filePath)))
            .OrderBy(Path.GetFileName, StringComparer.OrdinalIgnoreCase)];

        if (sourceFiles.Count == 0)
        {
            progress?.Report(new RenameProgressUpdate(0, 0, "No supported image files were found in the input folder."));
            return new RenameImagesResult(0, 0, 0, []);
        }

        int copiedFiles = 0;
        int skippedFiles = 0;
        int paddingWidth = Math.Max(2, sourceFiles.Count.ToString(CultureInfo.InvariantCulture).Length);
        List<string> outputFiles = [];

        for (int index = 0; index < sourceFiles.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            string sourceFile = sourceFiles[index];
            string destinationFileName = BuildDestinationFileName(prefix, index + 1, paddingWidth, sourceFile);
            string destinationFilePath = Path.Combine(outputFolder, destinationFileName);
            int processedCount = index + 1;
            bool destinationExistsBeforeCopy = File.Exists(destinationFilePath);

            if (destinationExistsBeforeCopy && !request.OverwriteExisting)
            {
                skippedFiles++;
                progress?.Report(new RenameProgressUpdate(
                    processedCount,
                    sourceFiles.Count,
                    $"Skipped '{destinationFileName}' because it already exists in the output folder."));
                continue;
            }

            await CopyAsync(sourceFile, destinationFilePath, request.OverwriteExisting, cancellationToken);

            copiedFiles++;
            outputFiles.Add(destinationFilePath);

            string action = request.OverwriteExisting && destinationExistsBeforeCopy
                ? "Saved"
                : "Copied";

            progress?.Report(new RenameProgressUpdate(
                processedCount,
                sourceFiles.Count,
                $"{action} '{destinationFileName}' from '{Path.GetFileName(sourceFile)}'."));
        }

        return new RenameImagesResult(sourceFiles.Count, copiedFiles, skippedFiles, outputFiles);
    }

    /// <summary>
    /// Asynchronously copies the contents of a file to a new location,
    /// with optional overwrite and cancellation support.
    /// </summary>
    /// <remarks>If overwriteExisting is false and the destination file already exists,
    /// an IOException is thrown. The method uses asynchronous I/O for efficient file copying
    /// and supports cancellation via the provided token.</remarks>
    /// <param name="sourceFilePath">The full path of the file to copy.
    /// Cannot be null or empty.</param>
    /// <param name="destinationFilePath">The full path of the destination file.
    /// Cannot be null or empty.</param>
    /// <param name="overwriteExisting">true to overwrite the destination file if it exists;
    /// otherwise, false to throw an exception if the file already exists.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the copy operation.</param>
    /// <returns>A task that represents the asynchronous copy operation.</returns>
    private static async Task CopyAsync(
        string sourceFilePath,
        string destinationFilePath,
        bool overwriteExisting,
        CancellationToken cancellationToken)
    {
        FileMode destinationFileMode = overwriteExisting ? FileMode.Create : FileMode.CreateNew;

        await using FileStream sourceStream = new(
            sourceFilePath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await using FileStream destinationStream = new(
            destinationFilePath,
            destinationFileMode,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            FileOptions.Asynchronous | FileOptions.SequentialScan);

        await sourceStream.CopyToAsync(destinationStream, cancellationToken);
    }

    /// <summary>
    /// Builds a destination file name by combining a specified prefix,
    /// a zero-padded number, and the original file extension.
    /// </summary>
    /// <remarks>The generated file name does not include any directory information;
    /// only the file name and extension are returned.
    /// The method uses the invariant culture for number formatting.</remarks>
    /// <param name="prefix">The prefix to include at the beginning of the generated file name.</param>
    /// <param name="number">The numeric value to include in the file name.
    /// This value is zero-padded to the specified width.</param>
    /// <param name="paddingWidth">The total number of digits to use for zero-padding the number.
    /// Must be greater than zero.</param>
    /// <param name="sourceFilePath">The full path of the source file.
    /// The file extension from this path is preserved in the generated file name.</param>
    /// <returns>A string representing the new file name, consisting of the prefix, an underscore,
    /// the zero-padded number, and the original file extension.</returns>
    private static string BuildDestinationFileName(
        string prefix,
        int number,
        int paddingWidth,
        string sourceFilePath)
    {
        string extension = Path.GetExtension(sourceFilePath);
        string numberToken = number.ToString($"D{paddingWidth}", CultureInfo.InvariantCulture);

        return $"{prefix}_{numberToken}{extension}";
    }

    /// <summary>
    /// Determines whether two file system paths refer to the same location,
    /// using platform-appropriate comparison rules.
    /// </summary>
    /// <remarks>
    /// On Windows and macOS, the comparison is case-insensitive.
    /// On other platforms, the comparison is case-sensitive.
    /// Both paths are normalized to their absolute forms before comparison.
    /// </remarks>
    /// <param name="left">The first file system path to compare.
    /// Can be relative or absolute.</param>
    /// <param name="right">The second file system path to compare.
    /// Can be relative or absolute.</param>
    /// <returns>true if the specified paths refer to the same location;
    /// otherwise, false.</returns>
    private static bool PathsMatch(
        string left,
        string right)
    {
        StringComparison comparison = OperatingSystem.IsWindows() || OperatingSystem.IsMacOS()
            ? StringComparison.OrdinalIgnoreCase
            : StringComparison.Ordinal;

        return string.Equals(
            Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar),
            Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar),
            comparison);
    }

    /// <summary>
    /// Validates that the specified folder path is not null,
    /// empty, or whitespace, and returns its absolute path.
    /// </summary>
    /// <param name="folderPath">The folder path to validate and convert to an absolute path.
    /// Cannot be null, empty, or consist only of white-space characters.</param>
    /// <param name="argumentName">The name of the parameter to include in the exception if validation fails.</param>
    /// <returns>The absolute path corresponding to the specified folder path.</returns>
    /// <exception cref="ArgumentException">Thrown if the folder path is null, empty,
    /// or consists only of white-space characters.</exception>
    private static string ValidateFolderPath(string folderPath, string argumentName)
    {
        return string.IsNullOrWhiteSpace(folderPath)
            ? throw new ArgumentException("Both the input and output folders are required.", argumentName)
            : Path.GetFullPath(folderPath.Trim());
    }

    /// <summary>
    /// Validates and sanitizes a file name prefix to ensure it is
    /// suitable for use in file renaming operations.
    /// </summary>
    /// <param name="prefix">The prefix to validate and sanitize for use as part of a file name.
    /// Cannot be null, empty, or contain invalid file name characters.</param>
    /// <returns>A trimmed version of the specified prefix that is safe to use in file names.</returns>
    /// <exception cref="ArgumentException">Thrown if the prefix is null, empty,
    /// consists only of white-space characters, or contains characters that are
    /// not valid in file names.</exception>
    private static string ValidatePrefix(string prefix)
    {
        if (string.IsNullOrWhiteSpace(prefix))
        {
            throw new ArgumentException("Enter a prefix before starting the rename operation.", nameof(prefix));
        }

        string sanitizedPrefix = prefix.Trim();
        char[] invalidCharacters = Path.GetInvalidFileNameChars();

        return sanitizedPrefix.IndexOfAny(invalidCharacters) >= 0
            ? throw new ArgumentException("The prefix contains characters that are not valid in file names.", nameof(prefix))
            : sanitizedPrefix;
    }
}
