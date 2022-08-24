using System.Xml;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetCatalogue
    {
        private static readonly HttpClient httpClient = new();
        public async Task<HttpResponseMessage> GetCatalogueEndpoint(string baseUrl, string accessToken, string subscriptionKey)
        {
            string uri = $"{baseUrl}/catalogues/1";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            if (!string.IsNullOrEmpty(accessToken))
            {
                httpRequestMessage.Headers.Add("token", accessToken);
            }
            if (!string.IsNullOrEmpty(subscriptionKey))
            {
                httpRequestMessage.Headers.Add("Ocp-Apim-Subscription-Key", subscriptionKey);
            }
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }
        public async Task<List<string>> GetProductList(HttpResponseMessage httpResponse)
        {
            List<string> productIdentifiers = new();

            if (httpResponse.IsSuccessStatusCode)
            {
                using (Stream stream = httpResponse.Content.ReadAsStream())
                {
                    XmlReaderSettings settings = new()
                    {
                        Async = true,
                        IgnoreWhitespace = true
                    };

                    using (XmlReader reader = XmlReader.Create(stream, settings))
                    {
                        while (await reader.ReadAsync())
                        {
                            if (reader.Name == "ShortName")
                            {
                                _ = reader.Read();
                                if (reader.HasValue)
                                {
                                    productIdentifiers.Add(reader.Value);
                                }
                            }
                        }
                    }
                }
            }
            return productIdentifiers;
        }
    }
}
