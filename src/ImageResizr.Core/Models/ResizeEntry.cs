namespace ImageResizr.Core.Models;

/// <summary>
/// Describes the result of processing one source image.
/// </summary>
/// <param name="FileName">The source image file name.</param>
/// <param name="OriginalWidth">The original display width, when decoding succeeded.</param>
/// <param name="OriginalHeight">The original display height, when decoding succeeded.</param>
/// <param name="NewWidth">The resulting width, when available.</param>
/// <param name="NewHeight">The resulting height, when available.</param>
/// <param name="Status">The processing outcome.</param>
/// <param name="OutputPath">The output path, when an output file exists.</param>
/// <param name="SkipReason">The reason the image was skipped.</param>
/// <param name="ErrorMessage">Technical error details for diagnostics.</param>
public sealed record ResizeEntry(
    string FileName,
    int? OriginalWidth,
    int? OriginalHeight,
    int? NewWidth,
    int? NewHeight,
    ResizeStatus Status,
    string? OutputPath = null,
    ResizeSkipReason SkipReason = ResizeSkipReason.None,
    string? ErrorMessage = null);
