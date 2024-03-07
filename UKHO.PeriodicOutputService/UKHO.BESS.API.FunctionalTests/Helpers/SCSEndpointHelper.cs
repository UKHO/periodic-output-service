namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class SCSEndpointHelper
    {
        static readonly HttpClient httpClient = new();
        static string? uri;

        /// <summary>
        /// This Method is used to execute Scs EssData endpoint
        /// </summary>
        /// <param name="baseUrl">sets the SCS baseUrl</param>
        /// <param name="accessToken">sets the token</param>
        /// <param name="validUri">default true to set the vald uri and false to set the incorrect uri</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ScsEssDataEndpoint(string? baseUrl, string? accessToken = null, bool validUri = true)
        {
            if (validUri)
            {
                uri = $"{baseUrl}/v1/productData/encs57/catalogue/essData";
            }
            else
            {
                uri = $"{baseUrl}/v1/productData/encs57/catalogue/essData123";
            }

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
