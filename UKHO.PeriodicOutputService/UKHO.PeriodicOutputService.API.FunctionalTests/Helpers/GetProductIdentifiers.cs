using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetProductIdentifiers
    {
        static HttpClient httpClient = new HttpClient();
        public async Task<HttpResponseMessage> GetProductIdentifiersDataAsync(string baseUrl, List<string> productIdentifierModel, string accessToken = null)
        {
            string uri = $"{baseUrl}/productData/productIdentifiers";
            
            string payloadJson = JsonConvert.SerializeObject(productIdentifierModel);

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
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
