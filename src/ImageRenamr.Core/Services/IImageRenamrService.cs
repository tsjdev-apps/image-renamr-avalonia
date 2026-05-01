using ImageRenamr.Core.Models;

namespace ImageRenamr.Core.Services;

public interface IImageRenamrService
{
    /// <summary>
    /// Renames image files in a specified folder according to
    /// the provided request parameters, performing the operation
    /// asynchronously.
    /// </summary>
    /// <param name="request">An object that specifies the folder,
    /// renaming pattern, and other options for the image renaming operation.
    /// Cannot be null.</param>
    /// <param name="progress">An optional progress reporter that receives
    /// updates about the renaming progress. If null, progress updates are
    /// not reported.</param>
    /// <param name="cancellationToken">A token that can be used to cancel
    /// the operation. The default value does not request cancellation.</param>
    /// <returns>A task that represents the asynchronous operation.
    /// The task result contains a summary of the renaming operation,
    /// including the number of files renamed and the output files.</returns>
    Task<RenameImagesResult> RenameAsync(
        RenameImagesRequest request,
        IProgress<RenameProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
