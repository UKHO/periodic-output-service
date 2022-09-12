using System.Xml;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class CommonHelper
    {
        public static async Task<string> DeserializeAsyncToken(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            dynamic response = JsonConvert.DeserializeObject<dynamic>(bodyJson);
            return response.token;

        }

        public static async Task<string> DeserializeAsyncMessage(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            dynamic response = JsonConvert.DeserializeObject<dynamic>(bodyJson);
            return response.message;
        }
        public static async Task<string> ReadAsStringAsync(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            return bodyJson;
        }

        public static dynamic XmlReadAsynch(string xmlreponse)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlreponse);
            string jsonText = JsonConvert.SerializeXmlNode(doc);
            dynamic deserializeJsonText = JsonConvert.DeserializeObject(jsonText);
            return deserializeJsonText;
        }

        public static string GetBase64EncodedCredentials(string username, string password)
        {
            var userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(username + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }

        public static async Task<dynamic> DeserializeAsyncResponse(this HttpResponseMessage httpResponseMessage)
        {
            string bodyJson = await httpResponseMessage.Content.ReadAsStringAsync();
            dynamic response = JsonConvert.DeserializeObject<dynamic>(bodyJson);
            return response;
        }

        public static async Task CallDelay()
        {
            await Task.Delay(120000);
        }
    }
}
