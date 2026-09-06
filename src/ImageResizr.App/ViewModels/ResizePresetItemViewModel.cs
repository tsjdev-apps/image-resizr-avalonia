using ImageResizr.App.Localization;
using ImageResizr.Core.Models;

namespace ImageResizr.App.ViewModels;

/// <summary>
/// Provides localized display text for a resize preset.
/// </summary>
/// <param name="Preset">The underlying resize preset.</param>
/// <param name="DisplayName">The localized preset name.</param>
public sealed record ResizePresetItemViewModel(
    ResizePresetOption Preset,
    string DisplayName)
{
    /// <summary>
    /// Gets the localized preset description.
    /// </summary>
    public string Description => Preset.IsCustom
        ? Strings.Preset_CustomDescription
        : Strings.Format("Preset_Description", Preset.Width, Preset.Height);

    /// <summary>
    /// Gets whether this preset accepts custom dimensions.
    /// </summary>
    public bool IsCustom => Preset.IsCustom;
}
