using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Models;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider
    {
        private readonly IOptions<EssManagedIdentityConfiguration> _essManagedIdentityConfiguration;
        private readonly ILogger<AuthTokenProvider> _logger;
        private static readonly object _lock = new();
        private readonly IDistributedCache _cache;

        public AuthTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration, IDistributedCache cache, ILogger<AuthTokenProvider> logger)
        {
            _essManagedIdentityConfiguration = essManagedIdentityConfiguration;
            _cache = cache;
            _logger = logger;
        }

        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            ////var accessToken = GetAuthTokenFromCache(resource);

            ////if (accessToken != null && accessToken.AccessToken != null && accessToken.ExpiresIn > DateTime.UtcNow)
            ////{
            ////    return accessToken.AccessToken;
            ////}

            ////var newAccessToken = await GetNewAuthToken(resource);
            ////AddAuthTokenToCache(resource, newAccessToken);

            ////return newAccessToken.AccessToken;

            return "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSIsImtpZCI6IjJaUXBKM1VwYmpBWVhZR2FYRUpsOGxWMFRPSSJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjU3NjMzNjIzLCJuYmYiOjE2NTc2MzM2MjMsImV4cCI6MTY1NzYzODk5MywiYWNyIjoiMSIsImFpbyI6IkFZUUFlLzhUQUFBQWk5MnNZMEFHem9UcW9Xd2VJT2dTYTZZTDhBV2dOY1RNMkpXQllkam4rclBzRHRXdTBvMmEremU3T0VwbmplTDlORE9vMDdmblBLa1hXRGJHSXdMWFVhL3A5am9mVXZZaVVSYVNUQkxrbHR1Y3pidzFwSWJsbHpkRjFtMXVJQWVjWUUwQldaOU8rVE5raE9Zc2lOZVZ4Zms3cjl5a2JZaGh0dldqUDZlYzd1VT0iLCJhbXIiOlsicHdkIiwibWZhIl0sImFwcGlkIjoiODBhNmM2OGItNTlhYS00OWE0LTkzOWEtNzk2OGZmNzlkNjc2IiwiYXBwaWRhY3IiOiIwIiwiZW1haWwiOiJWaXNoYWwxNDU4M0BtYXN0ZWsuY29tIiwiaWRwIjoiaHR0cHM6Ly9zdHMud2luZG93cy5uZXQvYWRkMWM1MDAtYTZkNy00ZGJkLWI4OTAtN2Y4Y2I2ZjdkODYxLyIsImlwYWRkciI6IjEwMy4xNjcuMTc2LjE4OCIsIm5hbWUiOiJWaXNoYWwgRHVrYXJlIiwib2lkIjoiODZjNWVlN2EtOTRlNi00OGM2LTlmOWMtYTRhYzU3MDRkZTFiIiwicmgiOiIwLkFRSUFTTW8wa1QxbUJVcVdpakdrTHdydFBvdkdwb0NxV2FSSms1cDVhUDk1MW5ZQ0FCOC4iLCJyb2xlcyI6WyJFeGNoYW5nZVNldFNlcnZpY2VVc2VyIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6IkljLURaS2FBTjZWX0liQmVZNXA3OFFrWVdOdjdsTlFFZnNzVk9TaU4wcVUiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6IlZpc2hhbDE0NTgzQG1hc3Rlay5jb20iLCJ1dGkiOiIyT1ZENjZsRkNVMjZ6RU9Vb3BBR0FBIiwidmVyIjoiMS4wIn0.XxnyLS7fm1Rp0tsfxOzw0hJFn23GROK07aq3SNeAon2JIz8m8DYbt_GAlZG9TeT0H3g-7WSAue5vcqXRgUrbqrhogouHG9coeoPOMYO9BQbxOEjy6MvgkRxqP1X7bM9MAYdSSKroY_halThIitnNlNFPWX8a0dyOFq_S8mrxpRdOTkz8OH3yah0YiRhFJzK40YS3jEaQ2uFWfSSyBU530ona7qQoJaWsAxG_lYpVg7-2Mt6b75fwWe3Zbd68ikIxeSaegSue9OMEv9nyFw0bNyDfz9Musx00k1frxx-zd2VW5HKx_7SJKYKqTfietmzSWp5sdJ3_nUervsEFzE4cRw";
            
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
                ////logger.LogInformation(EventIds.CachingExternalEndPointToken.ToEventId(), "Caching new token for external end point resource {resource} and expires in {ExpiresIn} with sliding expiration duration {options}.", key, Convert.ToString(accessTokenItem.ExpiresIn), JsonConvert.SerializeObject(options));
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
