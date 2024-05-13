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
        /// This method is used to get the response from pks endpoint.
        /// </summary>
        /// <param name="baseUrl">Sets the PKS baseUrl</param>
        /// <param name="pksData">Sets the data to get permit as per products provided</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> PermitKeysEndpoint(string? baseUrl, List<ProductKeyServiceModel> pksData)
        {
            uri = $"{baseUrl}/keys/ENC-S63";
            string payloadJson = JsonConvert.SerializeObject(pksData);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
    }
}
