namespace ImageResizr.Core.Models;

/// <summary>
/// Defines the severity level of a resize progress update.
/// </summary>
public enum ResizeProgressLevel
{
    /// <summary>
    /// Indicates an informational progress update.
    /// </summary>
    Info,

    /// <summary>
    /// Indicates that an image was processed successfully.
    /// </summary>
    Success,

    /// <summary>
    /// Indicates that an image was skipped or needs user attention.
    /// </summary>
    Warning,

    /// <summary>
    /// Indicates that processing failed for an image or operation.
    /// </summary>
    Error
}
