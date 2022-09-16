using System.Net;
using System.Xml;
using FluentAssertions;
using Newtonsoft.Json;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    
    public static class CommonHelper
    {
        private static POSWebJob WebJob;
        private static HttpResponseMessage POSWebJobApiResponse;
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
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

        public static async Task RunWebJob()
        {
            WebJob = new POSWebJob();
            if (!posWebJob.IsRunningOnLocalMachine)
            {
                string POSWebJobUserCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(posWebJob.UserName, posWebJob.Password);
                POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(posWebJob.BaseUrl, POSWebJobUserCredentialsBytes);
                POSWebJobApiResponse.StatusCode.Should().Be((HttpStatusCode)202);

                //As there is no way to check if webjob execution is completed or not, we have added below delay to wait till the execution completes and files get downloaded.
                await Task.Delay(70000);
            }
        }
    }
}

