using System.Collections.Specialized;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using ImageRenamr.App.Resources.Localization;
using ImageRenamr.App.ViewModels;

namespace ImageRenamr.App.Views;

public partial class MainWindow : Window
{
    private MainWindowViewModel? subscribedViewModel;

    /// <summary>
    /// Initializes a new instance of the MainWindow class.
    /// </summary>
    /// <remarks>This constructor sets up the main window and prepares it for user interaction. Typically
    /// called by the application framework when the window is created.</remarks>
    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Initializes a new instance of the MainWindow class with the specified view model as its data context.
    /// </summary>
    /// <remarks>This constructor assigns delegates to the view model for folder picking operations, enabling
    /// the view model to invoke platform-specific folder pickers without referencing UI types directly.</remarks>
    /// <param name="viewModel">The view model to associate with this window. Cannot be null.</param>
    public MainWindow(MainWindowViewModel viewModel)
        : this()
    {
        ArgumentNullException.ThrowIfNull(viewModel);

        DataContext = viewModel;

        // Assign delegates so the ViewModel can invoke platform folder pickers without referencing UI types.
        viewModel.PickInputFolderDelegate = async () =>
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Strings.InputFolder_PickerTitle
                });

            return folders.Count > 0 ? folders[0].TryGetLocalPath() ?? string.Empty : string.Empty;
        };

        viewModel.PickOutputFolderDelegate = async () =>
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = Strings.OutputFolder_PickerTitle
                });

            return folders.Count > 0 ? folders[0].TryGetLocalPath() ?? string.Empty : string.Empty;
        };
    }

    /// <inheritdoc/>
    protected override void OnDataContextChanged(EventArgs e)
    {
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.RenameHistory.CollectionChanged -= RenameHistoryOnCollectionChanged;
        }

        base.OnDataContextChanged(e);

        subscribedViewModel = DataContext as MainWindowViewModel;
        if (subscribedViewModel is not null)
        {
            subscribedViewModel.RenameHistory.CollectionChanged += RenameHistoryOnCollectionChanged;
        }
    }

    private void RenameHistoryOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add
            || subscribedViewModel is not { IsBusy: true }
            || subscribedViewModel.RenameHistory.Count == 0)
        {
            return;
        }

        RenameEntry newestEntry = subscribedViewModel.RenameHistory[^1];
        Dispatcher.UIThread.Post(
            () => RenameHistoryList.ScrollIntoView(newestEntry),
            DispatcherPriority.Background);
    }
}
