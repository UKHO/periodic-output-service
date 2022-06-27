using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

        public static async Task<dynamic> XmlReadAsynch(string xmlreponse)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xmlreponse);
            string jsonText = JsonConvert.SerializeXmlNode(doc);
            dynamic test = JsonConvert.DeserializeObject(jsonText);
            return test;
        }

        public static string getbase64encodedcredentials(string username, string password)
        {
            var userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(username + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }
    }
}
