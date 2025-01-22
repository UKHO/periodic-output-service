using Azure.Core;
using Azure.Identity;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class AuthTokenProvider
    {
        private static string EssAccessToken = null;
        private static string FssAccessToken = null;

        private static readonly ESSApiConfiguration EssAuthConfig = new TestConfiguration().EssConfig;
        private static readonly FSSApiConfiguration FssAuthConfig = new TestConfiguration().FssConfig;

        public static string GetEssToken()
        {
            GenerateToken(EssAuthConfig.TenantId, EssAuthConfig.AutoTestClientId, EssAuthConfig.AutoTestClientSecret, EssAuthConfig.EssClientId, ref EssAccessToken);
            return EssAccessToken;
        }

        public static string GetFssToken()
        {
            GenerateToken(EssAuthConfig.TenantId, EssAuthConfig.AutoTestClientId, EssAuthConfig.AutoTestClientSecret, FssAuthConfig.FssClientId, ref FssAccessToken);
            return FssAccessToken;
        }

        private static void GenerateToken(string tennantId, string clientId, string clientSecret, string scope, ref string token)
        {
            if (token == null)
            {
                ClientSecretCredential csc = new ClientSecretCredential(tennantId, clientId, clientSecret);
                token = csc.GetToken(new TokenRequestContext([$"{scope}/.default"])).Token;
            }
        }

    }
}
