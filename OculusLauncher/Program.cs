using Avalonia;
using OculusLauncher.Services;
using OculusLauncher.ViewModels;
using System;

namespace OculusLauncher;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Last-resort cleanup: if the process is killed externally,
        // remove any active firewall rule so it doesn't persist.
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            CleanupAllRules();
        };

        try
        {
            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
        }
        catch
        {
            CleanupAllRules();
            throw;
        }
    }

    private static void CleanupAllRules()
    {
        try
        {
            var fw = new FirewallService();
            if (fw.RuleExists(MainWindowViewModel.FirewallRuleName))
                fw.RemoveBlockRule(MainWindowViewModel.FirewallRuleName);
            // Also clean up legacy rule from older "EchoLauncher" version
            if (fw.RuleExists("EchoLauncher_OculusBlock"))
                fw.RemoveBlockRule("EchoLauncher_OculusBlock");
        }
        catch
        {
            // Best effort
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
