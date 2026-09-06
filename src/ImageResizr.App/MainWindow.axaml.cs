using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageResizr.App.Localization;
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

        viewModel.PickInputFolderDelegate = () => PickFolderAsync(Strings.SourceFolder_PickerTitle);
        viewModel.PickOutputFolderDelegate = () => PickFolderAsync(Strings.TargetFolder_PickerTitle);
        viewModel.ResizeHistory.CollectionChanged += ResizeHistoryOnCollectionChanged;
        Closed += (_, _) => viewModel.ResizeHistory.CollectionChanged -= ResizeHistoryOnCollectionChanged;
    }

    /// <summary>
    /// Keeps the most recently completed image visible while a batch is running.
    /// </summary>
    private void ResizeHistoryOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add
            || e.NewItems is not { Count: > 0 })
        {
            return;
        }

        object newestItem = e.NewItems[^1]!;
        Dispatcher.UIThread.Post(() => HistoryList.ScrollIntoView(newestItem));
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
