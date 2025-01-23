using Microsoft.Identity.Client;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class AuthTokenProvider
    {
        private static string EssAccessToken = null;
        private static string FssAccessToken = null;

        private static readonly ESSApiConfiguration EssauthConfig = new TestConfiguration().EssConfig;
        private static readonly FSSApiConfiguration FssAuthConfig = new TestConfiguration().FssConfig;

        public static async Task<string> GetEssToken()
        {
            EssAccessToken = await GenerateEssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, EssAccessToken);
            return EssAccessToken;
        }

        private static async Task<string> GenerateEssToken(string ClientId, string ClientSecret, string Token)
        {
            string[] scopes = new string[] { $"{EssauthConfig.EssClientId}/.default" };
            if (Token == null)
            {
                if (EssauthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(EssauthConfig.EssClientId).
                                                        WithRedirectUri("http://localhost").Build();
                    
                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .WithTenantIdFromAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }

        public static async Task<string> GetFssToken()
        {
            FssAccessToken = await GenerateFssToken(EssauthConfig.AutoTestClientId, EssauthConfig.AutoTestClientSecret, FssAccessToken);
            return FssAccessToken;
        }

        private static async Task<string> GenerateFssToken(string ClientId, string ClientSecret, string Token)
        {
            string[] scopes = new string[] { $"{FssAuthConfig.FssClientId}/.default" };
            if (Token == null)
            {
                if (FssAuthConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(FssAuthConfig.FssClientId).
                                                        WithRedirectUri("http://localhost").Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .WithTenantIdFromAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }
    }

}
