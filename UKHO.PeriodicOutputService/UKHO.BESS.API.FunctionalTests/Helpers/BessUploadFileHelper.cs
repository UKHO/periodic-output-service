using System.Text;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class BessUploadFileHelper
    {
        static readonly HttpClient httpClient = new();

        /// <summary>
        /// This method is use to upload the config file to storage.
        /// </summary>
        /// <param name="baseUrl">Sets the baseUrl of the endpoint</param>
        /// <param name="path">Use to pass the correct config</param>
        /// <param name="value">Sets the Authorization value</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> UploadConfigFile(string? baseUrl, string? path, string? value)
        {
            var uri = $"{baseUrl}/bessConfigUpload?Key={value}";
            var payloadJson = JsonConvert.SerializeObject(GetPayload(path));
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This method is to deserialize the payload
        /// </summary>
        /// <param name="path">Use to pass the correct config</param>
        /// <returns></returns>
        public static BessConfig? GetPayload(string? path)
        {
            return path != null ? JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText(path)) : null;
        }
    }
}
