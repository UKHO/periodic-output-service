using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class Extensions
    {
        public static void SetBearerToken(this HttpRequestMessage requestMessage, string accessToken)
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        /// <summary>
        /// Reads response body json as given type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="httpResponseMessage"></param>
        /// <returns></returns>
        public static async Task<T> ReadAsTypeAsync<T>(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(bodyJson);
        }
    }
}
