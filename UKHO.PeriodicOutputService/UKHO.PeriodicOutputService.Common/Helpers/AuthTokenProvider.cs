using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Options;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class AuthTokenProvider : IAuthTokenProvider
    {
        private readonly IOptions<EssManagedIdentityConfiguration> _essManagedIdentityConfiguration;

        public AuthTokenProvider(IOptions<EssManagedIdentityConfiguration> essManagedIdentityConfiguration)
        {
            _essManagedIdentityConfiguration = essManagedIdentityConfiguration;
        }
        public async Task<string> GetManagedIdentityAuthAsync(string essClientId)
        {
            return await GetNewAuthToken(essClientId);
        }

        private async Task<string> GetNewAuthToken(string essClientId)
        {
            var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = _essManagedIdentityConfiguration.Value.ClientId });
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { essClientId + "/.default" }) { }
            );

            return accessToken.Token;
        }
    }
}
