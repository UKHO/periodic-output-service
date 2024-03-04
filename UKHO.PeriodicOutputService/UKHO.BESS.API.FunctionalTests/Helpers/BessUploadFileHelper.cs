using System.Text;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class BessUploadFileHelper
    {
        static readonly HttpClient httpClient = new();
        public static async Task<HttpResponseMessage> UploadConfigFile(string baseUrl, string path)
        {
            var uri = $"{baseUrl}/bessConfigUpload";
            var payloadJson = JsonConvert.SerializeObject(GetPayload(path));
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") })
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public static List<BessConfig>? GetPayload(string path)
        {
            return path != null ? JsonConvert.DeserializeObject<List<BessConfig>>(File.ReadAllText(path)) : null;
        }
    }
}
