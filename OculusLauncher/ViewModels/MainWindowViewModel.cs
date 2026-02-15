using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using OculusLauncher.Models;
using OculusLauncher.Services;

namespace OculusLauncher.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public const string FirewallRuleName = "OculusLauncher_Block";
    private const string LegacyFirewallRuleName = "EchoLauncher_OculusBlock";

    private static readonly string[] BlockedDomains =
    [
        "graph.oculus.com",
        "www.oculus.com"
    ];

    private readonly IFirewallService _firewall;
    private readonly IOculusProcessService _oculusProcess;
    private readonly IDnsResolverService _dnsResolver;
    private readonly ISettingsService _settings;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isRunning;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(LaunchCommand))]
    [NotifyCanExecuteChangedFor(nameof(StopCommand))]
    private bool _isWorking;

    [ObservableProperty]
    private string _statusMessage = "Ready";

    [ObservableProperty]
    private string _blockedIpsText = "";

    [ObservableProperty]
    private string _customOculusPath = "";

    public ObservableCollection<string> LogMessages { get; } = new();

    /// <summary>
    /// Set by the View to provide file dialog functionality.
    /// </summary>
    public Func<Task<string?>>? BrowseForFileAsync { get; set; }

    public MainWindowViewModel()
        : this(new FirewallService(), new OculusProcessService(), new DnsResolverService(), new SettingsService())
    {
    }

    public MainWindowViewModel(
        IFirewallService firewall,
        IOculusProcessService oculusProcess,
        IDnsResolverService dnsResolver,
        ISettingsService settings)
    {
        _firewall = firewall;
        _oculusProcess = oculusProcess;
        _dnsResolver = dnsResolver;
        _settings = settings;

        // Load saved settings
        var appSettings = _settings.Load();
        _customOculusPath = appSettings.CustomOculusClientPath ?? "";

        // Clean up any stale rules from a previous crash
        CleanupStaleRules();
    }

    private bool CanLaunch() => !IsRunning && !IsWorking;
    private bool CanStop() => IsRunning && !IsWorking;

    [RelayCommand(CanExecute = nameof(CanLaunch))]
    private async Task LaunchAsync()
    {
        IsWorking = true;
        try
        {
            // Step 1: Find Oculus path
            Log("Searching for Oculus client...");
            var manualPath = string.IsNullOrWhiteSpace(CustomOculusPath) ? null : CustomOculusPath;
            var oculusPath = _oculusProcess.FindOculusClientPath(manualPath);
            if (oculusPath == null)
            {
                if (manualPath != null)
                    Log($"ERROR: Custom path not found: {manualPath}");
                else
                    Log("ERROR: Could not find Oculus client. Use Browse to set the path manually.");
                StatusMessage = "Error: Oculus client not found";
                return;
            }
            Log($"Found: {oculusPath}");

            // Step 2: Kill Oculus if running
            if (_oculusProcess.IsOculusRunning())
            {
                Log("Stopping Oculus client...");
                StatusMessage = "Stopping Oculus...";
                _oculusProcess.KillOculusClient();
                await Task.Delay(2000);
                Log("Oculus client stopped.");
            }

            // Step 3: Resolve DNS
            Log("Resolving Meta API domains...");
            StatusMessage = "Resolving DNS...";
            var ips = await _dnsResolver.ResolveDomainsAsync(BlockedDomains);
            for (int i = 0; i < BlockedDomains.Length; i++)
            {
                Log($"  {BlockedDomains[i]} -> {ips[i]}");
            }
            BlockedIpsText = string.Join(", ", ips);

            // Step 4: Create firewall rule
            Log("Creating firewall block rule...");
            StatusMessage = "Creating firewall rule...";
            _firewall.CreateBlockRule(FirewallRuleName, ips);
            Log("Firewall rule created. Meta API is BLOCKED.");

            // Step 5: Start Oculus
            Log("Starting Oculus client...");
            StatusMessage = "Starting Oculus...";
            _oculusProcess.StartOculusClient(oculusPath);
            Log("Oculus client started.");

            IsRunning = true;
            StatusMessage = "Active - Meta API blocked";
            Log("Ready! Click Stop when you're done to restore API access.");
        }
        catch (Exception ex)
        {
            Log($"ERROR: {ex.Message}");
            StatusMessage = $"Error: {ex.Message}";
            TryRemoveFirewallRule();
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand(CanExecute = nameof(CanStop))]
    private void Stop()
    {
        IsWorking = true;
        try
        {
            Log("Removing firewall block rule...");
            StatusMessage = "Removing firewall rule...";
            _firewall.RemoveBlockRule(FirewallRuleName);
            Log("Firewall rule removed. Meta API access restored.");

            IsRunning = false;
            BlockedIpsText = "";
            StatusMessage = "Stopped - API access restored";
        }
        catch (Exception ex)
        {
            Log($"WARNING: Failed to remove firewall rule: {ex.Message}");
            Log("You may need to manually remove 'OculusLauncher_Block' from Windows Firewall.");
            StatusMessage = "Warning: cleanup failed";
        }
        finally
        {
            IsWorking = false;
        }
    }

    [RelayCommand]
    private async Task BrowseOculusPathAsync()
    {
        if (BrowseForFileAsync == null) return;

        var path = await BrowseForFileAsync();
        if (!string.IsNullOrWhiteSpace(path))
        {
            CustomOculusPath = path;
            SaveSettings();
            Log($"Custom Oculus path set: {path}");
        }
    }

    [RelayCommand]
    private void ClearOculusPath()
    {
        CustomOculusPath = "";
        SaveSettings();
        Log("Custom path cleared. Using auto-detect.");
    }

    public void ForceCleanup()
    {
        TryRemoveFirewallRule();
    }

    private void SaveSettings()
    {
        var appSettings = new AppSettings
        {
            CustomOculusClientPath = string.IsNullOrWhiteSpace(CustomOculusPath) ? null : CustomOculusPath
        };
        _settings.Save(appSettings);
    }

    private void CleanupStaleRules()
    {
        // Clean up current rule name
        if (_firewall.RuleExists(FirewallRuleName))
        {
            try
            {
                _firewall.RemoveBlockRule(FirewallRuleName);
                Log("Cleaned up stale firewall rule from previous session.");
            }
            catch { }
        }

        // Clean up legacy rule name from old "EchoLauncher" version
        if (_firewall.RuleExists(LegacyFirewallRuleName))
        {
            try
            {
                _firewall.RemoveBlockRule(LegacyFirewallRuleName);
                Log("Cleaned up legacy firewall rule from older version.");
            }
            catch { }
        }
    }

    private void TryRemoveFirewallRule()
    {
        try { _firewall.RemoveBlockRule(FirewallRuleName); } catch { }
    }

    private void Log(string message)
    {
        var entry = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogMessages.Add(entry);
    }
}
