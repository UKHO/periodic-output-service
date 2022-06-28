using Azure.Core;
using Azure.Identity;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class AuthTokenProvider : IAuthTokenProvider
    {
        public async Task<string> GetManagedIdentityAuthAsync(string resource)
        {
            return await GetNewAuthToken(resource);
        }

        private async Task<string> GetNewAuthToken(string resource)
        {
            var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = resource });
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { resource + "/.default" }) { }
            );

            return accessToken.Token;
        }
    }
}
