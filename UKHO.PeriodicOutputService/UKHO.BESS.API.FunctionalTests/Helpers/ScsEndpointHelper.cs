using Azure.Storage.Blobs.Specialized;

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
        /// <param name="validUri">Sets the valid or invalid uri. Default is true</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> EssDataEndpoint(string? baseUrl, bool validUri = true)
        {
            uri = validUri ? $"{baseUrl}/v1/productData/encs57/catalogue/essData" : $"{baseUrl}/v1/productData/encs57/catalogue/essData123";

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
