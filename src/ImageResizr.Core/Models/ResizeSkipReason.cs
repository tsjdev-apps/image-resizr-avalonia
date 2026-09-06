namespace ImageResizr.Core.Models;

/// <summary>
/// Describes why an image was skipped.
/// </summary>
public enum ResizeSkipReason
{
    /// <summary>No skip reason applies.</summary>
    None,

    /// <summary>An output file already exists and overwriting is disabled.</summary>
    OutputFileExists,

    /// <summary>The image already satisfies the requested dimensions.</summary>
    AlreadyCorrectSize
}
