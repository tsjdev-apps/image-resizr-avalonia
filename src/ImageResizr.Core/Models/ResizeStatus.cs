namespace ImageResizr.Core.Models;

/// <summary>
/// Describes the outcome of processing one image.
/// </summary>
public enum ResizeStatus
{
    /// <summary>The image was resized and written to a new output file.</summary>
    Resized,

    /// <summary>The image was resized and replaced an existing output file.</summary>
    Overwritten,

    /// <summary>The image was not resized.</summary>
    Skipped,

    /// <summary>The image could not be processed.</summary>
    Failed
}
