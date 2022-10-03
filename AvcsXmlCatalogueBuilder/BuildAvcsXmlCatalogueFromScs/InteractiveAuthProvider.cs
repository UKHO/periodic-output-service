using System.DirectoryServices.AccountManagement;
using AvcsXmlCatalogueBuilder;
using Microsoft.Identity.Client;
using Microsoft.Identity.Client.Extensions.Msal;

namespace BuildAvcsXmlCatalogueFromScs
{
    internal class InteractiveAuthProvider : IAuthTokenProvider
    {
        private readonly string clientId;
        private readonly IPublicClientApplication app;
        private readonly Task cacheHelperTask;
        private readonly string currentUserPrincipalName;

        public InteractiveAuthProvider(string clientId, string tenantId, string microsoftOnlineLoginUrl)
        {
            this.clientId = clientId;


            currentUserPrincipalName = UserPrincipal.Current.UserPrincipalName.ToLower().Replace("engineering.", "");

            var storageProperties = new StorageCreationPropertiesBuilder("token.cache", Environment.CurrentDirectory).Build();
            var authority = $"{microsoftOnlineLoginUrl}{tenantId}";

            app = PublicClientApplicationBuilder
                    .Create(clientId)
                    .WithAuthority(authority)
                    .WithRedirectUri("http://localhost")
                    .Build()
                ;
            cacheHelperTask = MsalCacheHelper.CreateAsync(storageProperties).ContinueWith(r =>
            {
                var cacheHelper = r.Result;
                cacheHelper.RegisterCache(app.UserTokenCache);
            });
        }

        public Task<string> GetAuthToken()
        {
            return GenerateToken();
        }

        private async Task<string> GenerateToken()
        {
            await cacheHelperTask;

            var scopes = new[] { $"{clientId}/user_impersonation" };
            
            try
            {
                var silentTokenResult = await app.AcquireTokenSilent(scopes, currentUserPrincipalName.ToLower()).ExecuteAsync();
                if (silentTokenResult.ExpiresOn > DateTimeOffset.UtcNow)
                    return silentTokenResult.AccessToken;
            }
            catch (MsalException)
            {
            }

            var authenticationResult = await app.AcquireTokenInteractive(scopes).ExecuteAsync();
            return authenticationResult.AccessToken;
        }
    }
}