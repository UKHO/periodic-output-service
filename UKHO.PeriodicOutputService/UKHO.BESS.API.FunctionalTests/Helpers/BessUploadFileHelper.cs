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
        /// <param name="baseUrl"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <param name="exchangeSetStandard"></param>
        /// <param name="type"></param>
        /// <param name="readMeSearchFilter"></param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> UploadConfigFile(string? baseUrl, string? path, string? value, string exchangeSetStandard, string type, string readMeSearchFilter)
        {
            var uri = $"{baseUrl}/bessConfigUpload?Key={value}";
            var configDetails = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText(path!));
            string payloadJson = GetPayload(configDetails!, exchangeSetStandard, type, readMeSearchFilter);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
                { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        ///  This method is to deserialize the payload
        /// </summary>
        /// <param name="configDetails"></param>
        /// <param name="exchangeSetStandard"></param>
        /// <param name="type"></param>
        /// <param name="readMeSearchFilter"></param>
        /// <returns></returns>
        public static string GetPayload(dynamic configDetails, string exchangeSetStandard, string type, string readMeSearchFilter)
        {
            configDetails!.Name = "BES-123" + Extensions.RandomNumber();
            configDetails!.Type = type;
            configDetails.ExchangeSetStandard = exchangeSetStandard;
            configDetails!.ReadMeSearchFilter = readMeSearchFilter;
            return JsonConvert.SerializeObject(configDetails);
        }
    }
}
