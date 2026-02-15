using System;
using System.Collections.Specialized;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using OculusLauncher.ViewModels;

namespace OculusLauncher.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        Closing += OnClosing;
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);

        if (DataContext is MainWindowViewModel vm)
        {
            // Auto-scroll log to bottom when new messages are added
            vm.LogMessages.CollectionChanged += (_, args) =>
            {
                if (args.Action == NotifyCollectionChangedAction.Add)
                {
                    LogScrollViewer?.ScrollToEnd();
                }
            };

            // Wire up file dialog for Browse button
            vm.BrowseForFileAsync = BrowseForOculusClientAsync;
        }
    }

    private async Task<string?> BrowseForOculusClientAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select Oculus Client Executable",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Executable Files") { Patterns = ["*.exe"] },
                new FilePickerFileType("All Files") { Patterns = ["*"] }
            ]
        });

        return files.FirstOrDefault()?.Path.LocalPath;
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (DataContext is MainWindowViewModel vm)
        {
            vm.ForceCleanup();
        }
    }
}
