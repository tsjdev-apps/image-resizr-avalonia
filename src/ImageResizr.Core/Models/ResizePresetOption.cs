namespace ImageResizr.Core.Models;

/// <summary>
/// Describes a selectable resize preset for the user interface.
/// </summary>
/// <param name="DisplayName">The display name shown for the preset.</param>
/// <param name="Width">The preset width in pixels.</param>
/// <param name="Height">The preset height in pixels.</param>
/// <param name="IsCustom">Whether the preset allows custom dimensions.</param>
public sealed record ResizePresetOption(
    string DisplayName,
    int Width,
    int Height,
    bool IsCustom = false)
{
    /// <summary>
    /// Gets the small landscape preset.
    /// </summary>
    public static ResizePresetOption Small { get; } = new("Small", 854, 480);

    /// <summary>
    /// Gets the medium landscape preset.
    /// </summary>
    public static ResizePresetOption Medium { get; } = new("Medium", 1366, 768);

    /// <summary>
    /// Gets the large landscape preset.
    /// </summary>
    public static ResizePresetOption Large { get; } = new("Large", 1920, 1080);

    /// <summary>
    /// Gets the phone portrait preset.
    /// </summary>
    public static ResizePresetOption Phone { get; } = new("Phone", 320, 568);

    /// <summary>
    /// Gets the custom dimensions preset.
    /// </summary>
    public static ResizePresetOption Custom { get; } = new("Custom", 1000, 1000, IsCustom: true);

    /// <summary>
    /// Gets all preset options in display order.
    /// </summary>
    public static IReadOnlyList<ResizePresetOption> All { get; } =
    [
        Small,
        Medium,
        Large,
        Phone,
        Custom
    ];

    /// <summary>
    /// Gets the human-readable preset description.
    /// </summary>
    public string Description => IsCustom
        ? "Use custom pixel dimensions"
        : $"Fits within {Width} x {Height} pixels";
}
