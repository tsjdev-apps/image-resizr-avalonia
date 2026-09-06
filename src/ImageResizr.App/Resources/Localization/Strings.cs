using System.Globalization;
using System.Resources;

namespace ImageResizr.App.Localization;

#pragma warning disable CA1707 // Resource keys deliberately use semantic underscore-separated names.
#pragma warning disable CS1591 // Individual resource accessors are self-describing generated-style members.

/// <summary>
/// Provides strongly named access to localized application strings.
/// </summary>
public static class Strings
{
    private static readonly ResourceManager Manager = new(
        "ImageResizr.App.Localization.Strings",
        typeof(Strings).Assembly);

    public static string Window_Title => Get(nameof(Window_Title));
    public static string Main_Title => Get(nameof(Main_Title));
    public static string Main_Description => Get(nameof(Main_Description));
    public static string SourceFolder_Label => Get(nameof(SourceFolder_Label));
    public static string SourceFolder_Watermark => Get(nameof(SourceFolder_Watermark));
    public static string SourceFolder_PickerTitle => Get(nameof(SourceFolder_PickerTitle));
    public static string TargetFolder_Label => Get(nameof(TargetFolder_Label));
    public static string TargetFolder_Watermark => Get(nameof(TargetFolder_Watermark));
    public static string TargetFolder_PickerTitle => Get(nameof(TargetFolder_PickerTitle));
    public static string Browse_ToolTip => Get(nameof(Browse_ToolTip));
    public static string ResizeOptions_Title => Get(nameof(ResizeOptions_Title));
    public static string SizePreset_Label => Get(nameof(SizePreset_Label));
    public static string Width_Label => Get(nameof(Width_Label));
    public static string Height_Label => Get(nameof(Height_Label));
    public static string Unit_Label => Get(nameof(Unit_Label));
    public static string Pixels_Label => Get(nameof(Pixels_Label));
    public static string ResizeMode_Label => Get(nameof(ResizeMode_Label));
    public static string ShrinkOnly_Label => Get(nameof(ShrinkOnly_Label));
    public static string IgnoreOrientation_Label => Get(nameof(IgnoreOrientation_Label));
    public static string OverwriteFiles_Label => Get(nameof(OverwriteFiles_Label));
    public static string ResizeImages_Button => Get(nameof(ResizeImages_Button));
    public static string ResizingImages_Button => Get(nameof(ResizingImages_Button));
    public static string Progress_Title => Get(nameof(Progress_Title));
    public static string Progress_OverallTitle => Get(nameof(Progress_OverallTitle));
    public static string Progress_HistoryTitle => Get(nameof(Progress_HistoryTitle));
    public static string Progress_NoFiles => Get(nameof(Progress_NoFiles));
    public static string Status_Ready => Get(nameof(Status_Ready));
    public static string Status_Preparing => Get(nameof(Status_Preparing));
    public static string Status_Running => Get(nameof(Status_Running));
    public static string Status_Completed => Get(nameof(Status_Completed));
    public static string Status_NoSupportedFiles => Get(nameof(Status_NoSupportedFiles));
    public static string Status_ReviewSettings => Get(nameof(Status_ReviewSettings));
    public static string Status_InputFolderMissing => Get(nameof(Status_InputFolderMissing));
    public static string Status_FileSystemError => Get(nameof(Status_FileSystemError));
    public static string Status_AccessDenied => Get(nameof(Status_AccessDenied));
    public static string Preset_Small => Get(nameof(Preset_Small));
    public static string Preset_Medium => Get(nameof(Preset_Medium));
    public static string Preset_Large => Get(nameof(Preset_Large));
    public static string Preset_Phone => Get(nameof(Preset_Phone));
    public static string Preset_Custom => Get(nameof(Preset_Custom));
    public static string Preset_CustomDescription => Get(nameof(Preset_CustomDescription));
    public static string ResizeMode_Fit => Get(nameof(ResizeMode_Fit));
    public static string ResizeMode_Fill => Get(nameof(ResizeMode_Fill));
    public static string ResizeMode_Stretch => Get(nameof(ResizeMode_Stretch));

    /// <summary>
    /// Gets a resource by semantic key using standard .NET UI-culture fallback.
    /// </summary>
    public static string Get(string name, CultureInfo? culture = null)
    {
        return Manager.GetString(name, culture ?? CultureInfo.CurrentUICulture)
            ?? throw new MissingManifestResourceException($"The localization resource '{name}' is missing.");
    }

    /// <summary>
    /// Formats a localized resource using the current UI culture.
    /// </summary>
    public static string Format(string name, params object?[] arguments)
    {
        return string.Format(CultureInfo.CurrentCulture, Get(name), arguments);
    }
}

#pragma warning restore CS1591
#pragma warning restore CA1707
