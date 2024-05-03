using System.Text;
using Newtonsoft.Json;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class PksEndpointHelper
    {
        static readonly HttpClient httpClient = new();
        private static string? uri;

        /// <summary>
        /// This method is used to generate the permits.
        /// </summary>
        /// <param name="baseUrl"></param>
        /// <param name="productName"></param>
        /// <param name="editionNumber"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PermitKeysEndpoint(string? baseUrl, List<ProductKeyServiceModel> pksData)
        {
            uri = $"{baseUrl}/keys/ENC-S63";

            string payloadJson = JsonConvert.SerializeObject(pksData);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)

            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
