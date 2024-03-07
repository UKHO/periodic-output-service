using System.Net.Http.Headers;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        /// <summary>
        /// This Method is set the bearer token
        /// </summary>
        /// <param name="requestMessage">Represents a HTTP request message</param>
        /// <param name="accessToken">Set the access token</param>
        public static void SetBearerToken(this HttpRequestMessage requestMessage, string accessToken)
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }
    }
}
