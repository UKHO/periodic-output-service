
using System.Globalization;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        static readonly HttpClient httpClient = new HttpClient();
        private static string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
        private static string currentYear = DateTime.UtcNow.ToString("yy");
        private static List<string> expectedFileName = new List<string>();
        public static async Task<HttpResponseMessage> GetBatchDetailsEndpoint(string baseUrl, string batchId)
        {
            string uri = $"{baseUrl}/batch/{batchId}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public static void GetBatchDetailsResponseValidation(dynamic batchDetailsResponse)
        {
            string expectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");
            //to check status
            string batchStatus = batchDetailsResponse.status;
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Expected Batch Status of Committed, but actual value is {batchStatus}");

            string businessUnit = batchDetailsResponse.businessUnit;
            Assert.That(businessUnit, Is.EqualTo("AVCSData"), $"Expected Business Unit of AVCSData, but actual value is {businessUnit}");

            string expiryDate = batchDetailsResponse.expiryDate;
            Assert.That(expiryDate.Contains(expectedExpiryDate), $"Expected Expiry Date to contain {expectedExpiryDate}, but actual value is {expiryDate}");
        }

        public static void GetBatchDetailsResponseValidationForFullAVCSExchangeSet(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[1].value;

            if (mediaType.Equals("Zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals(string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear)))
                {
                    Assert.That(fileName, Is.EqualTo(string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear)), $"Expected Response File Name Zip of {posDetails.UpdateExchangeSet}, but actual value is {fileName}");
                }
                else
                {
                    int responseFileNameContent = 0;
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        var folderName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        Assert.That(responseFileNameZip, Is.EqualTo(folderName), $"Expected Response File Name Zip of {folderName}, but actual value is {responseFileNameZip}");

                        responseFileNameContent++;
                    }
                }
            }
            else if (mediaType.Equals("DVD"))
            {
                for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                {
                    var folderNameIso = string.Format(posDetails.PosAvcsIsoFileName, dvdNumber, weekNumber, currentYear);
                    var FolderNameSha1 = string.Format(posDetails.PosAvcsIsoSha1FileName, dvdNumber, weekNumber, currentYear);
                    expectedFileName.Add(folderNameIso);
                    expectedFileName.Add(FolderNameSha1);
                }
                 
                
                for (int responseFileNameLocation = 0; responseFileNameLocation < expectedFileName.Count; responseFileNameLocation++)
                {
                    string responseFileName = batchDetailsResponse.files[responseFileNameLocation].filename;
                    Assert.That(responseFileName, Is.EqualTo(expectedFileName[responseFileNameLocation]), $"Expected Response File Name of {expectedFileName[responseFileNameLocation]}, but actual value is {responseFileName}");
                }
            }
            else
            {
                Assert.Fail($"{mediaType} is different then Zip & DVD");
            }
        }
    }
}
