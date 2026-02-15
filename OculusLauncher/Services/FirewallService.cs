using System;
using System.Collections.Generic;
using System.Linq;

namespace OculusLauncher.Services;

public class FirewallService : IFirewallService
{
    // COM constants for HNetCfg.FWRule
    private const int NET_FW_IP_PROTOCOL_TCP = 6;
    private const int NET_FW_RULE_DIR_OUT = 2;
    private const int NET_FW_ACTION_BLOCK = 0;
    private const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;

    public void CreateBlockRule(string ruleName, IEnumerable<string> remoteAddresses)
    {
        var addresses = remoteAddresses.ToList();
        if (addresses.Count == 0)
            throw new ArgumentException("No remote addresses provided.");

        // Remove any existing rule with the same name first
        try { RemoveBlockRule(ruleName); } catch { /* ignore if not found */ }

        dynamic fwPolicy = CreateFwPolicy();
        dynamic fwRule = CreateFwRule();

        fwRule.Name = ruleName;
        fwRule.Description = "Temporary Oculus API block - created by Oculus Launcher";
        fwRule.Protocol = NET_FW_IP_PROTOCOL_TCP;
        fwRule.Direction = NET_FW_RULE_DIR_OUT;
        fwRule.Action = NET_FW_ACTION_BLOCK;
        fwRule.RemotePorts = "443";
        fwRule.RemoteAddresses = string.Join(",", addresses);
        fwRule.Profiles = NET_FW_PROFILE2_ALL;
        fwRule.Enabled = true;

        fwPolicy.Rules.Add(fwRule);
    }

    public void RemoveBlockRule(string ruleName)
    {
        dynamic fwPolicy = CreateFwPolicy();
        fwPolicy.Rules.Remove(ruleName);
    }

    public bool RuleExists(string ruleName)
    {
        try
        {
            dynamic fwPolicy = CreateFwPolicy();
            foreach (dynamic rule in fwPolicy.Rules)
            {
                if (rule.Name == ruleName)
                    return true;
            }
            return false;
        }
        catch
        {
            return false;
        }
    }

    private static object CreateFwPolicy()
    {
        var type = Type.GetTypeFromProgID("HNetCfg.FwPolicy2")
            ?? throw new InvalidOperationException("Windows Firewall COM API not available.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Failed to create firewall policy instance.");
    }

    private static object CreateFwRule()
    {
        var type = Type.GetTypeFromProgID("HNetCfg.FWRule")
            ?? throw new InvalidOperationException("Windows Firewall COM API not available.");
        return Activator.CreateInstance(type)
            ?? throw new InvalidOperationException("Failed to create firewall rule instance.");
    }
}
