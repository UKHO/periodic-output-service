using System.Text;
using Newtonsoft.Json;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class EssEndpointHelper
    {
        static readonly HttpClient httpClient = new();
        private static string? uri;

        /// <summary>
        /// This Method is used to execute ESS Product Identifier endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the ESS baseUrl</param>
        /// <param name="productIdentifier">Sets the products to get data</param>
        /// <param name="exchangeSetStandard">Sets the type of ExchangeStandard required</param>
        /// <param name="validUri">Sets the valid or invalid uri. Default is true</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ProductIdentifiersEndpoint(string? baseUrl, List<string> productIdentifier, string exchangeSetStandard = "s63", bool validUri = true)
        {
            uri = validUri ? $"{baseUrl}/productData/productIdentifiers?exchangeSetStandard={exchangeSetStandard}" : $"{baseUrl}/productData/productIdentifiers123";

            string payloadJson = JsonConvert.SerializeObject(productIdentifier);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)

            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }

        }

        /// <summary>
        /// This Method is used to execute ESS Product Version endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the ESS baseUrl</param>
        /// <param name="productVersion">Sets the product and its version to get data</param>
        /// <param name="exchangeSetStandard">Sets the type of ExchangeStandard required</param>
        /// <param name="validUri">Sets the valid or invalid uri. Default is true</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ProductVersionsEndpoint(string? baseUrl, List<ProductVersionModel> productVersion, string exchangeSetStandard = "s63", bool validUri = true )
        {
            uri = validUri ? $"{baseUrl}/productData/productVersions?exchangeSetStandard={exchangeSetStandard}" : $"{baseUrl}/productData/productIdentifiers123";

            string payloadJson = JsonConvert.SerializeObject(productVersion);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)

            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })                                                        
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
