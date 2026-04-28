namespace ImageResizr.Core.Models;

/// <summary>
/// Describes a batch image resize operation.
/// </summary>
/// <param name="InputFolder">The folder that contains the source images.</param>
/// <param name="OutputFolder">The folder where resized images should be written.</param>
/// <param name="TargetWidth">The requested output width in pixels.</param>
/// <param name="TargetHeight">The requested output height in pixels.</param>
/// <param name="ResizeMode">The resize behavior to apply.</param>
/// <param name="ShrinkOnly">Whether images should only be reduced in size.</param>
/// <param name="IgnoreOrientation">Whether EXIF orientation should be ignored.</param>
/// <param name="OverwriteExisting">Whether existing output files should be replaced.</param>
public sealed record ResizeImagesRequest(
    string InputFolder,
    string OutputFolder,
    int TargetWidth,
    int TargetHeight,
    ImageResizeMode ResizeMode,
    bool ShrinkOnly,
    bool IgnoreOrientation,
    bool OverwriteExisting);
