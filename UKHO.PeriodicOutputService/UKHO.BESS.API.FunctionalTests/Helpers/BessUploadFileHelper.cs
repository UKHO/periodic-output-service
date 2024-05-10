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
        /// <param name="baseUrl">Sets the baseUrl of container storage to upload config</param>
        /// <param name="path">Sets the path for the config file to be uploaded</param>
        /// <param name="value">Sets the value of the key parameter in the uri</param>
        /// <param name="exchangeSetStandard">Sets the value of exchangeSetStandard in config out of s63 or s57</param>
        /// <param name="type">Sets the value of the Exchange Set Type in config out of BASE, UPDATE or CHANGE</param>
        /// <param name="readMeSearchFilter">Sets the value of the Readme File type in config out of AVCS, BLANK or {Query}</param>
        /// <param name="keyFileType">Sets the value of the Permit File Type in config out of KEY_TEXT or PERMIT_XML</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> UploadConfigFile(string? baseUrl, string? path, string? value, string exchangeSetStandard, string type, string readMeSearchFilter, string keyFileType)
        {
            var uri = $"{baseUrl}/bessConfigUpload?Key={value}";
            var configDetails = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText(path!));
            string payloadJson = GetPayload(configDetails!, exchangeSetStandard, type, readMeSearchFilter, keyFileType);
            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This method is to deserialize the payload
        /// </summary>
        /// <param name="configDetails">Stores the read config details</param>
        /// <param name="exchangeSetStandard">Sets the value of exchangeSetStandard in payload out of s63 or s57</param>
        /// <param name="type">Sets the value of the Exchange Set Type in payload out of BASE, UPDATE or CHANGE</param>
        /// <param name="readMeSearchFilter">Sets the value of the Readme File type in payload out of AVCS, BLANK or {Query}</param>
        /// <param name="keyFileType">Sets the value of the Permit File Type in payload out of KEY_TEXT or PERMIT_XML</param>
        /// <returns></returns>
        public static string GetPayload(dynamic configDetails, string exchangeSetStandard, string type, string readMeSearchFilter, string keyFileType)
        {
            configDetails.Type = type;
            configDetails.ExchangeSetStandard = exchangeSetStandard;
            configDetails.ReadMeSearchFilter = readMeSearchFilter;
            configDetails.KeyFileType = keyFileType;
            return JsonConvert.SerializeObject(configDetails);
        }
    }
}
