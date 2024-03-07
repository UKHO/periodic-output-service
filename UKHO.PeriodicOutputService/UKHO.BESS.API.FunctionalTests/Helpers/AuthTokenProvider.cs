using Microsoft.Identity.Client;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class AuthTokenProvider
    {
        static string? ScsAccessToken = null;
        static readonly TestConfiguration testConfiguration = new();

        /// <summary>
        /// This method is use to get the SCS Token
        /// </summary>
        /// <returns></returns>
        public async Task<string> GetScsToken()
        {
            ScsAccessToken = await GenerateScsToken(testConfiguration.authTokenConfig.AutoTestClientId, testConfiguration.authTokenConfig.AutoTestClientSecret, ScsAccessToken);
            return ScsAccessToken;
        }

        /// <summary>
        /// Generate SCS Token
        /// </summary>
        /// <param name="ClientId">sets the clientId</param>
        /// <param name="ClientSecret">sets the clientSecret key to generate the token in pipeline</param>
        /// <param name="Token">sets the Token</param>
        /// <returns></returns>
        private static async Task<string> GenerateScsToken(string? ClientId, string? ClientSecret, string? Token)
        {
            string[] scopes = new string[] { $"{testConfiguration.scsConfig.ResourceId}/.default" };
            if (Token == null)
            {
                if (testConfiguration.scsConfig.IsRunningOnLocalMachine)
                {
                    IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(testConfiguration.scsConfig.ResourceId).
                                                        WithRedirectUri("http://localhost").Build();

                    //Acquiring token through user interaction
                    AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                                                            .WithAuthority($"{testConfiguration.authTokenConfig.MicrosoftOnlineLoginUrl}{testConfiguration.authTokenConfig.TenantId}", true)
                                                            .ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }
                else
                {
                    IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(ClientId)
                                                    .WithClientSecret(ClientSecret)
                                                    .WithAuthority(new Uri($"{testConfiguration.authTokenConfig.MicrosoftOnlineLoginUrl}{testConfiguration.authTokenConfig.TenantId}"))
                                                    .Build();

                    AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                    Token = tokenTask.AccessToken;
                }

            }
            return Token;
        }
    }
}
