﻿using System.Diagnostics.CodeAnalysis;
using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage] ////Excluded from code coverage as it has ADD interaction
    public class AuthTokenProvider : IAuthEssTokenProvider, IAuthFssTokenProvider, IAuthScsTokenProvider, IAuthPksTokenProvider
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

        public async Task<string> GetManagedIdentityAuthAsync(string resource, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.GetAccessTokenStarted.ToEventId(), "Getting access token to call external endpoint started | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            AccessTokenItem? accessToken = GetAuthTokenFromCache(resource);
            if (accessToken != null && accessToken.AccessToken != null && accessToken.ExpiresIn > DateTime.UtcNow)
            {
                _logger.LogInformation(EventIds.CachedAccessTokenFound.ToEventId(), "Valid access token found in cache to call external endpoint | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
                return accessToken.AccessToken;
            }

            AccessTokenItem? newAccessToken = await GetNewAuthToken(resource, correlationId);
            AddAuthTokenToCache(resource, newAccessToken, correlationId);

            _logger.LogInformation(EventIds.GetAccessTokenCompleted.ToEventId(), "Getting access token to call external endpoint completed | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));
            return newAccessToken.AccessToken;
        }

        private async Task<AccessTokenItem> GetNewAuthToken(string resource, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.GetNewAccessTokenStarted.ToEventId(), "Generating new access token to call external endpoint started | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            var tokenCredential = new DefaultAzureCredential();
            AccessToken accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: [resource + "/.default"]) { }
            );

            _logger.LogInformation(EventIds.GetNewAccessTokenCompleted.ToEventId(), "New access token to call external endpoint generated successfully | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            return new AccessTokenItem
            {
                ExpiresIn = accessToken.ExpiresOn.UtcDateTime,
                AccessToken = accessToken.Token
            };
        }

        private void AddAuthTokenToCache(string key, AccessTokenItem accessTokenItem, string? correlationId = null)
        {
            _logger.LogInformation(EventIds.CachingExternalEndPointTokenStarted.ToEventId(), "Adding new access token in cache to call external endpoint | {DateTime} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.GetCorrelationId(correlationId));

            double tokenExpiryMinutes = accessTokenItem.ExpiresIn.Subtract(DateTime.UtcNow).TotalMinutes;
            double deductTokenExpiryMinutes = _essManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes < tokenExpiryMinutes ? _essManagedIdentityConfiguration.Value.DeductTokenExpiryMinutes : 1;
            DistributedCacheEntryOptions? options = new();
            options.SetAbsoluteExpiration(accessTokenItem.ExpiresIn.Subtract(TimeSpan.FromMinutes(deductTokenExpiryMinutes)));

            lock (_lock)
            {
                _cache.SetString(key, JsonConvert.SerializeObject(accessTokenItem), options);
            }
            _logger.LogInformation(EventIds.CachingExternalEndPointTokenCompleted.ToEventId(), "New token is added in cache to call external endpoint and it expires in {ExpiresIn} with sliding expiration duration {options} | _X-Correlation-ID:{CorrelationId}.", Convert.ToString(accessTokenItem.ExpiresIn), JsonConvert.SerializeObject(options), CommonHelper.GetCorrelationId(correlationId));
        }

        private AccessTokenItem GetAuthTokenFromCache(string key)
        {
            string? item;
            lock (_lock)
            {
                item = _cache.GetString(key);
            }
            return item != null ? JsonConvert.DeserializeObject<AccessTokenItem>(item) : null;
        }
    }
}
