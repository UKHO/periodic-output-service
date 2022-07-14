using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics.CodeAnalysis;
using UKHO.PeriodicOutputService.Common.Configuration;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has AD interaction
    public class AuthFssTokenProvider : AuthTokenProvider, IAuthFssTokenProvider
    {
        public AuthFssTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration, IDistributedCache _cache, ILogger<AuthFssTokenProvider> logger) :
            base(essManagedIdentityConfiguration, _cache)
        {
        }
    }
}
