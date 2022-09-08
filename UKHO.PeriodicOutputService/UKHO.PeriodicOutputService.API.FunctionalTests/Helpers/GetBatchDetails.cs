
using System.Globalization;
using NUnit.Framework;
using FluentAssertions;
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
            batchStatus.Should().Be("Committed");

            string businessUnit = batchDetailsResponse.businessUnit;
            businessUnit.Should().Be("AVCSData");

            string expiryDate = batchDetailsResponse.expiryDate;
            expiryDate.Should().Contain(expectedExpiryDate);
        }

        public static void GetBatchDetailsResponseValidationForFullAVCSExchangeSet(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[6].value;
            if (mediaType.Equals("Zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Contains("UPDATE"))
                {
                    fileName.Should().Be(string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear));
                }
                else
                {
                    int responseFileNameContent = 0;
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        var folderName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        responseFileNameZip.Should().Be(folderName);
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
                    responseFileName.Should().Be(expectedFileName[responseFileNameLocation]);
                }
            }
            else
            {
                mediaType.Should().ContainAny("Zip","DVD");
            }
        }

        public static void GetBatchDetailsResponseValidationForCatalogueXmlOrEncUpdateListCsv(dynamic batchDetailsResponse)
        {
            string responseContent = batchDetailsResponse.attributes[5].value;
            string responseFileName = batchDetailsResponse.files[0].filename;

            switch (responseContent)
            {
                case "Catalogue":
                    responseFileName.Should().Be(posDetails.AVCSCatalogueFileName);
                    break;
                case "ENC Updates":
                    responseFileName.Should().Be(posDetails.EncUpdateListFileName);
                    break;
                default:
                    responseContent.Should().ContainAny("Catalogue.xml", "Enc Update list.csv");
                    break;
            }
        }
    }
}
