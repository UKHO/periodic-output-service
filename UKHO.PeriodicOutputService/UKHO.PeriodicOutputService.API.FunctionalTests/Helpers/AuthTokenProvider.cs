using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Identity.Client;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;
namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class AuthTokenProvider
    {
        static string EssAccessToken = null;
        static EssAuthorizationConfiguration EssauthConfig = new TestConfiguration().EssAuthorizationConfig;
        
        public async Task<string> GetEssToken()
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
                                                            .WithAuthority($"{EssauthConfig.MicrosoftOnlineLoginUrl}{EssauthConfig.TenantId}", true)
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
