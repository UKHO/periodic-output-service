using System.Globalization;
using System.Net;
using System.Text;
using System.Xml;
using Newtonsoft.Json;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class CommonHelper
    {
        private static POSWebJob s_webJob;
        private static HttpResponseMessage s_posWebJobApiResponse;
        private static readonly POSWebJobApiConfiguration s_posWebJobApiConfiguration = new TestConfiguration().POSWebJobConfig;
        private static readonly AioWebjobApiConfiguration s_aioWebJobApiConfiguration = new TestConfiguration().AioWebjobApiConfig;

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
            XmlDocument doc = new();
            doc.LoadXml(xmlreponse);
            string jsonText = JsonConvert.SerializeXmlNode(doc);
            dynamic deserializeJsonText = JsonConvert.DeserializeObject(jsonText);
            return deserializeJsonText;
        }

        public static string GetBase64EncodedCredentials(string username, string password)
        {
            var userCredentialsBytes = Encoding.UTF8.GetBytes(username + ":" + password);
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
            s_webJob = new POSWebJob();
            if (!s_posWebJobApiConfiguration.IsRunningOnLocalMachine)
            {
                string POSWebJobUserCredentialsBytes = GetBase64EncodedCredentials(s_posWebJobApiConfiguration.UserName, s_posWebJobApiConfiguration.Password);
                s_posWebJobApiResponse = await s_webJob.POSWebJobEndPoint(s_posWebJobApiConfiguration.BaseUrl, POSWebJobUserCredentialsBytes);
                Assert.That(s_posWebJobApiResponse.StatusCode, Is.EqualTo((HttpStatusCode)202));

                //As there is no way to check if webjob execution is completed or not, we have added below delay to wait till the execution completes and files get downloaded.
                await Task.Delay(s_posWebJobApiConfiguration.WebjobRunningStatusDelayTime);
            }
        }

        public static async Task RunWebJobAio()
        {
            s_webJob = new POSWebJob();
            if (!s_posWebJobApiConfiguration.IsRunningOnLocalMachine)
            {
                string POSWebJobUserCredentialsBytes = GetBase64EncodedCredentials(s_posWebJobApiConfiguration.UserName, s_posWebJobApiConfiguration.Password);
                s_posWebJobApiResponse = await s_webJob.POSWebJobEndPoint(s_aioWebJobApiConfiguration.BaseUrl, POSWebJobUserCredentialsBytes);
                Assert.That(s_posWebJobApiResponse.StatusCode, Is.EqualTo((HttpStatusCode)202));

                //As there is no way to check if webjob execution is completed or not, we have added below delay to wait till the execution completes and files get downloaded.
                await Task.Delay(s_aioWebJobApiConfiguration.WebjobRunningStatusDelayTime);
            }
        }

        public static (string CurrentWeek, string CurrentYear, string CurrentYearShort) GetCurrentWeekAndYear() => GetCurrentWeekAndYearCommon(DateTime.UtcNow);

        public static (string CurrentWeek, string CurrentYear, string CurrentYearShort) GetCurrentWeekAndYearAio() => GetCurrentWeekAndYearCommon(DateTime.UtcNow.AddDays(s_aioWebJobApiConfiguration.WeeksToIncrement * 7));

        private static (string CurrentWeek, string CurrentYear, string CurrentYearShort) GetCurrentWeekAndYearCommon(DateTime now)
        {
            var currentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
            var currentYear = now.Year;

            if (currentWeek > 5 && now.Month < 2)
            {
                currentYear--;
            }

            var currentYearFull = currentYear.ToString("0000");
            return (currentWeek.ToString("00"), currentYearFull, currentYearFull.Substring(2));
        }
    }
}

