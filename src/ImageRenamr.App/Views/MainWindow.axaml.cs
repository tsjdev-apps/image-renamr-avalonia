using Avalonia.Controls;
using Avalonia.Platform.Storage;
using ImageRenamr.App.ViewModels;

namespace ImageRenamr.App.Views;

public partial class MainWindow : Window
{
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
                    Title = "Choose the input folder"
                });

            return folders.Count > 0 ? folders[0].TryGetLocalPath() ?? string.Empty : string.Empty;
        };

        viewModel.PickOutputFolderDelegate = async () =>
        {
            IReadOnlyList<IStorageFolder> folders = await StorageProvider.OpenFolderPickerAsync(
                new FolderPickerOpenOptions
                {
                    AllowMultiple = false,
                    Title = "Choose the output folder"
                });

            return folders.Count > 0 ? folders[0].TryGetLocalPath() ?? string.Empty : string.Empty;
        };
    }
}
