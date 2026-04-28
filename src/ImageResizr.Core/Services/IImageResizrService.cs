using ImageResizr.Core.Models;

namespace ImageResizr.Core.Services;

/// <summary>
/// Provides batch image resizing operations.
/// </summary>
public interface IImageResizrService
{
    /// <summary>
    /// Resizes supported images from the input folder into the output folder.
    /// </summary>
    /// <param name="request">The resize request to execute.</param>
    /// <param name="progress">The optional progress reporter for user-facing updates.</param>
    /// <param name="cancellationToken">The token used to cancel the resize operation.</param>
    /// <returns>The resize operation result.</returns>
    Task<ResizeImagesResult> ResizeAsync(
        ResizeImagesRequest request,
        IProgress<ResizeProgressUpdate>? progress = null,
        CancellationToken cancellationToken = default);
}
