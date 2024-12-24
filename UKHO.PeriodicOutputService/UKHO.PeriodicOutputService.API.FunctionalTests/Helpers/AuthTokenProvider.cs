using Microsoft.Identity.Client;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class AuthTokenProvider
    {
        private static string EssAccessToken;
        private static string FssAccessToken;

        private static readonly ESSApiConfiguration EssauthConfig = new TestConfiguration().EssConfig;
        private static readonly FSSApiConfiguration FssAuthConfig = new TestConfiguration().FssConfig;

        public async Task<string> GetEssToken()
        {
            EssAccessToken = await GenerateEssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, EssAccessToken);
            return EssAccessToken;
        }

        private static async Task<string> GenerateEssToken(string clientId, string clientSecret, string token)
        {
            string[] scopes = new string[] { $"{EssauthConfig.EssClientId}/.default" };
            if (token == null)
            {
                if (EssauthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(EssauthConfig.EssClientId).
                                                        WithRedirectUri("http://localhost").Build();
                    
                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .WithTenantIdFromAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                            .ExecuteAsync();
                    token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                    .WithClientSecret(clientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    token = tokenTask.AccessToken;
                }

            }
            return token;
        }

        public async Task<string> GetFssToken()
        {
            FssAccessToken = await GenerateFssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, FssAccessToken);
            return FssAccessToken;
        }

        private static async Task<string> GenerateFssToken(string clientId, string clientSecret, string token)
        {
            string[] scopes = { $"{FssAuthConfig.FssClientId}/.default" };
            if (token == null)
            {
                if (FssAuthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(FssAuthConfig.FssClientId).
                                                        WithRedirectUri("http://localhost").Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .WithTenantIdFromAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                            .ExecuteAsync();
                    token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                                                    .WithClientSecret(clientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    token = tokenTask.AccessToken;
                }

            }
            return token;
        }
    }

}
