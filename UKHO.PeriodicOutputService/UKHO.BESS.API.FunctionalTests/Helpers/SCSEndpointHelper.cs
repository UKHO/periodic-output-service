namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class ScsEndpointHelper
    {
        static readonly HttpClient httpClient = new();
        private static string? uri;

        /// <summary>
        /// This Method is used to execute Scs EssData endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the SCS baseUrl</param>
        /// <param name="accessToken">Sets the token</param>
        /// <param name="validUri">Default true to set the valid uri and false to set the incorrect uri</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ScsEssDataEndpoint(string? baseUrl, string? accessToken = null, bool validUri = true)
        {
            uri = validUri ? $"{baseUrl}/v1/productData/encs57/catalogue/essData" : $"{baseUrl}/v1/productData/encs57/catalogue/essData123";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            if (accessToken != null)
            {
                httpRequestMessage.SetBearerToken(accessToken);
            }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
