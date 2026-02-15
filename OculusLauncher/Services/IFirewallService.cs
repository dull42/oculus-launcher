using System.Collections.Generic;

namespace OculusLauncher.Services;

public interface IFirewallService
{
    void CreateBlockRule(string ruleName, IEnumerable<string> remoteAddresses);
    void RemoveBlockRule(string ruleName);
    bool RuleExists(string ruleName);
}
