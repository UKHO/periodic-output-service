using System.Net.Http.Headers;

namespace UKHO.PeriodicOutputService.Common
{
    public static class Extensions
    {
        public static void SetBearerToken(this HttpRequestMessage requestMessage, string accessToken)
        {
            requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        public static void AddHeader(this HttpRequestMessage requestMessage, string name, string value)
        {
            requestMessage.Headers.Add(name, value);
        }
    }
}
