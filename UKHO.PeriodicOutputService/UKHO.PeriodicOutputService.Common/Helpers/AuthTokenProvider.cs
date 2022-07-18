using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider : IAuthEssTokenProvider, IAuthFssTokenProvider
    {
        private readonly IOptions<EssManagedIdentityConfiguration> _essManagedIdentityConfiguration;
        private static readonly object _lock = new();
        private readonly ILogger<AuthTokenProvider> _logger;
        private readonly IDistributedCache _cache;

        public AuthTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration,
                                 IDistributedCache cache,
                                 ILogger<AuthTokenProvider> logger)
        {
            _essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            var accessToken = GetAuthTokenFromCache(resource);

            if (accessToken != null && accessToken.AccessToken != null && accessToken.ExpiresIn > DateTime.UtcNow)
            {
                return accessToken.AccessToken;
            }

            var newAccessToken = await GetNewAuthToken(resource);
            AddAuthTokenToCache(resource, newAccessToken);

            return newAccessToken.AccessToken;
        }

        private async Task<AccessTokenItem> GetNewAuthToken(string resource)
        {
            var tokenCredential = new DefaultAzureCredential();
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { }
            );

            return new AccessTokenItem
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        private void AddAuthTokenToCache(string key, AccessTokenItem accessTokenItem)
        {
            var tokenExpiryMinutes = accessTokenItem.ExpiresIn.Subtract(DateTime.UtcNow).TotalMinutes;
            var deductTokenExpiryMinutes = _essManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes < tokenExpiryMinutes ? _essManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes : 1;
            var options = new DistributedCacheEntryOptions().SetSlidingExpiration(TimeSpan.FromMinutes(tokenExpiryMinutes - deductTokenExpiryMinutes));
            options.SetAbsoluteExpiration(accessTokenItem.ExpiresIn);

            lock (_lock)
            {
                _cache.SetString(key, JsonConvert.SerializeObject(accessTokenItem), options);
                _logger.LogInformation(EventIds.CachingExternalEndPointToken.ToEventId(), "Caching new token for external end point resource {resource} and expires in {ExpiresIn} with sliding expiration duration {options}.", key, Convert.ToString(accessTokenItem.ExpiresIn), JsonConvert.SerializeObject(options));
            }
        }

        private AccessTokenItem GetAuthTokenFromCache(string key)
        {
            var item = _cache.GetString(key);
            if (item != null)
            {
                return JsonConvert.DeserializeObject<AccessTokenItem>(item);
            }
            return null;
        }
    }
}
