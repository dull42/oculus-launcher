using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using OculusLauncher.Services;
using OculusLauncher.ViewModels;
using OculusLauncher.Views;

namespace OculusLauncher;

public partial class App : Application
{
    private MainWindowViewModel? _viewModel;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            _viewModel = new MainWindowViewModel(
                new FirewallService(),
                new OculusProcessService(),
                new DnsResolverService(),
                new SettingsService());

            desktop.MainWindow = new MainWindow
            {
                DataContext = _viewModel,
            };

            desktop.ShutdownRequested += (_, _) =>
            {
                _viewModel?.ForceCleanup();
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
