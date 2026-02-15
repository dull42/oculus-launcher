using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace OculusLauncher.Services;

public class DnsResolverService : IDnsResolverService
{
    public async Task<List<string>> ResolveDomainsAsync(IEnumerable<string> domains)
    {
        var ips = new List<string>();

        foreach (var domain in domains)
        {
            var addresses = await Dns.GetHostAddressesAsync(domain);
            var ipv4 = addresses
                .Where(a => a.AddressFamily == AddressFamily.InterNetwork)
                .Select(a => a.ToString())
                .FirstOrDefault();

            if (ipv4 == null)
                throw new Exception($"Could not resolve IPv4 address for {domain}");

            ips.Add(ipv4);
        }

        return ips;
    }
}
