namespace ImageResizr.Core.Models;

/// <summary>
/// Defines how an image should be resized relative to the requested dimensions.
/// </summary>
public enum ImageResizeMode
{
    /// <summary>
    /// Preserves the original aspect ratio and fits the image inside the target bounds.
    /// </summary>
    Fit,

    /// <summary>
    /// Preserves the original aspect ratio and crops the image to exactly fill the target bounds.
    /// </summary>
    Fill,

    /// <summary>
    /// Resizes the image to the exact target width and height without preserving the aspect ratio.
    /// </summary>
    Stretch
}
