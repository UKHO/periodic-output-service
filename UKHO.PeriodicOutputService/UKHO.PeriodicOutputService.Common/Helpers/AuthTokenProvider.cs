using Azure.Core;
using Azure.Identity;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class AuthTokenProvider : IAuthTokenProvider
    {
        public async Task<string> GetManagedIdentityAuthAsync(string essClientId, string managedIdentityClientId)
        {
            return await GetNewAuthToken(essClientId, managedIdentityClientId);
        }

        private async Task<string> GetNewAuthToken(string essClientId, string managedIdentityClientId)
        {
            var tokenCredential = new DefaultAzureCredential(new DefaultAzureCredentialOptions { ManagedIdentityClientId = managedIdentityClientId });
            var accessToken = await tokenCredential.GetTokenAsync(
                new TokenRequestContext(scopes: new string[] { essClientId + "/.default" }) { }
            );

            return accessToken.Token;
        }
    }
}
