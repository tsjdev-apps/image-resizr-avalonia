using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ImageResizr.App.ViewModels;

namespace ImageResizr.App;

/// <summary>
/// Hosts the main image resize user interface.
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindow" /> class with its view model.
    /// </summary>
    /// <param name="viewModel">The view model used as the data context.</param>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        DataContext = viewModel;

        viewModel.PickInputFolderDelegate = () => PickFolderAsync("Choose the input folder");
        viewModel.PickOutputFolderDelegate = () => PickFolderAsync("Choose the output folder");
    }

    /// <summary>
    /// Opens a folder picker and returns the selected local path.
    /// </summary>
    private async Task<string?> PickFolderAsync(string title)
    {
        IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
            new FolderPickerOpenOptions
            {
                AllowMultiple = false,
                Title = title
            });

        return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
    }
}
