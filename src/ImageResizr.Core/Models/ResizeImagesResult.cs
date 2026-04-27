namespace ImageResizr.Core.Models;

/// <summary>
/// Describes the outcome of a batch image resize operation.
/// </summary>
/// <param name="TotalFiles">The total number of supported source files discovered.</param>
/// <param name="ResizedFiles">The number of files that were resized.</param>
/// <param name="SkippedFiles">The number of files that were skipped.</param>
/// <param name="FailedFiles">The number of files that could not be processed.</param>
/// <param name="InputBytes">The total size of processed input files in bytes.</param>
/// <param name="OutputBytes">The total size of produced output files in bytes.</param>
/// <param name="OutputFiles">The paths to output files created or kept by the operation.</param>
public sealed record ResizeImagesResult(
    int TotalFiles,
    int ResizedFiles,
    int SkippedFiles,
    int FailedFiles,
    long InputBytes,
    long OutputBytes,
    IReadOnlyList<string> OutputFiles)
{
    /// <summary>
    /// Gets the estimated number of bytes saved by resizing the images.
    /// </summary>
    public long SavedBytes => Math.Max(0, InputBytes - OutputBytes);
}
