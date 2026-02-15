using System.Collections.Generic;
using System.Threading.Tasks;

namespace OculusLauncher.Services;

public interface IDnsResolverService
{
    Task<List<string>> ResolveDomainsAsync(IEnumerable<string> domains);
}
