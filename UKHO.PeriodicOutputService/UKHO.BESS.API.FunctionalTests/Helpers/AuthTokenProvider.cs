using Microsoft.Identity.Client;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class AuthTokenProvider
    {
        static string? ScsAccessToken;
        static readonly TestConfiguration testConfiguration = new();

        /// <summary>
        /// This method is use to get the SCS Token
        /// </summary>
        /// <returns></returns>
        public static async Task<string> GetScsToken()
        {
            ScsAccessToken = await GenerateScsToken(testConfiguration.authTokenConfig.AutoTestClientId, testConfiguration.authTokenConfig.AutoTestClientSecret, ScsAccessToken);
            return ScsAccessToken;
        }

        /// <summary>
        /// Generate SCS Token
        /// </summary>
        /// <param name="clientId">Sets the clientId</param>
        /// <param name="clientSecret">Sets the clientSecret key to generate the token in pipeline</param>
        /// <param name="token">Sets the token</param>
        /// <returns></returns>
        private static async Task<string> GenerateScsToken(string? clientId, string? clientSecret, string? token)
        {
            string[] scopes = new string[] { $"{testConfiguration.scsConfig.ResourceId}/.default" };
            if (token != null)
            {
                return token;
            }

            if (testConfiguration.scsConfig.IsRunningOnLocalMachine)
            {
                IPublicClientApplication debugApp = PublicClientApplicationBuilder.Create(testConfiguration.scsConfig.ResourceId).
                    WithRedirectUri("http://localhost").Build();

                //Acquiring token through user interaction
                AuthenticationResult tokenTask = await debugApp.AcquireTokenInteractive(scopes)
                    .WithAuthority($"{testConfiguration.authTokenConfig.MicrosoftOnlineLoginUrl}{testConfiguration.authTokenConfig.TenantId}", true)
                    .ExecuteAsync();
                token = tokenTask.AccessToken;
            }
            else
            {
                IConfidentialClientApplication app = ConfidentialClientApplicationBuilder.Create(clientId)
                    .WithClientSecret(clientSecret)
                    .WithAuthority(new Uri($"{testConfiguration.authTokenConfig.MicrosoftOnlineLoginUrl}{testConfiguration.authTokenConfig.TenantId}"))
                    .Build();

                AuthenticationResult tokenTask = await app.AcquireTokenForClient(scopes).ExecuteAsync();
                token = tokenTask.AccessToken;
            }
            return token;
        }
    }
}
