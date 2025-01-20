using System.Globalization;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly HttpClient httpClient = new();
        private static readonly string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
        private static readonly string currentYear = DateTime.UtcNow.ToString("yy");
        private static readonly List<string> expectedFileName = new();
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
            Assert.Equals(batchStatus, "Committed");

            string businessUnit = batchDetailsResponse.businessUnit;
            Assert.Equals(businessUnit, "AVCSData");

            string expiryDate = batchDetailsResponse.expiryDate;
            Assert.That(expiryDate.Contains(expectedExpiryDate));

            string fileSize = batchDetailsResponse.files[0].fileSize;
            Assert.That(!string.IsNullOrEmpty(fileSize));

            string hash = batchDetailsResponse.files[0].hash;
            Assert.That(!string.IsNullOrEmpty(hash));
        }

        public static void GetBatchDetailsResponseValidationForFullAVCSExchangeSet(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[5].value;
            if (mediaType.ToLower().Equals("zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Contains("UPDATE"))
                {
                    Assert.Equals(fileName, string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear));

                    string responseFileMimeName = batchDetailsResponse.files[0].mimeType;
                    Assert.Equals(responseFileMimeName, posDetails.ZipFileMimeType);
                }
                else
                {
                    int responseFileNameContent = 0;
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        string folderName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        Assert.Equals(responseFileNameZip, folderName);

                        string responseFileMimeName = batchDetailsResponse.files[responseFileNameContent].mimeType;
                        Assert.Equals(responseFileMimeName, posDetails.ZipFileMimeType);

                        string fileSize = batchDetailsResponse.files[responseFileNameContent].fileSize;
                        Assert.That(!string.IsNullOrEmpty(fileSize));

                        string hash = batchDetailsResponse.files[responseFileNameContent].hash;
                        Assert.That(!string.IsNullOrEmpty(hash));

                        responseFileNameContent++;
                    }
                }
            }
            else if (mediaType.Equals("DVD"))
            {
                for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                {
                    string folderNameIso = string.Format(posDetails.PosAvcsIsoFileName, dvdNumber, weekNumber, currentYear);
                    string FolderNameSha1 = string.Format(posDetails.PosAvcsIsoSha1FileName, dvdNumber, weekNumber, currentYear);
                    expectedFileName.Add(folderNameIso);
                    expectedFileName.Add(FolderNameSha1);
                }

                for (int responseFileNameLocation = 0; responseFileNameLocation < expectedFileName.Count; responseFileNameLocation++)
                {
                    string responseFileName = batchDetailsResponse.files[responseFileNameLocation].filename;
                    Assert.Equals(responseFileName, expectedFileName[responseFileNameLocation]);

                    string responseFileMimeName = batchDetailsResponse.files[responseFileNameLocation].mimeType;

                    Assert.Equals(responseFileMimeName, responseFileName.Contains(".sha1")
                        ? posDetails.Sha1FileMimeType
                        : posDetails.IsoFileMimeType);

                    string fileSize = batchDetailsResponse.files[responseFileNameLocation].fileSize;
                    Assert.That(!string.IsNullOrEmpty(fileSize));

                    string hash = batchDetailsResponse.files[responseFileNameLocation].hash;
                    Assert.That(!string.IsNullOrEmpty(hash));
                }
            }
            else 
            {
                Assert.That(mediaType.Contains("Zip") || mediaType.Contains("DVD"));
            }
        }

        public static void GetBatchDetailsResponseValidationForCatalogueXmlOrEncUpdateListCsv(dynamic batchDetailsResponse)
        {
            string responseContent = batchDetailsResponse.attributes[4].value;
            string responseFileName = batchDetailsResponse.files[0].filename;
            string responseFileMimeName = batchDetailsResponse.files[0].mimeType;

            switch (responseContent)
            {
                case "Catalogue":
                    Assert.Equals(responseFileName, posDetails.AVCSCatalogueFileName);
                    Assert.Equals(responseFileMimeName, posDetails.AVCSCatalogueFileMimeType);
                    break;
                case "ENC Updates":
                    Assert.Equals(responseFileName, posDetails.EncUpdateListFileName);
                    Assert.Equals(responseFileMimeName, posDetails.EncUpdateListFileMimeType);
                    break;
                default:
                    Assert.That(responseContent.Contains("Catalogue.xml") || responseContent.Contains("Enc Update list.csv"));
                    break;
            }
        }

        public static void GetBatchDetailsResponseValidationForAio(dynamic batchDetailsResponse, string exchangeSetType)
        {
            string expectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");
            string expectedWeekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string expectedYear = DateTime.UtcNow.Year.ToString();

            //to check status
            string actualBatchStatus = batchDetailsResponse.status;
            Assert.Equals(actualBatchStatus, "Committed");

            string actualBusinessUnit = batchDetailsResponse.businessUnit;
            Assert.Equals(actualBusinessUnit, "AVCSData");

            string actualExpiryDate = batchDetailsResponse.expiryDate;
            Assert.That(actualExpiryDate.Contains(expectedExpiryDate));

            string actualProductType = batchDetailsResponse.attributes[0].value;
            Assert.Equals(actualProductType, "AIO");

            string actualWeekNumber = batchDetailsResponse.attributes[1].value;
            Assert.Equals(actualWeekNumber, expectedWeekNumber);

            string actualYear = batchDetailsResponse.attributes[2].value;
            Assert.Equals(actualYear, expectedYear);

            string actualYearAndWeek = batchDetailsResponse.attributes[3].value;
            Assert.Equals(actualYearAndWeek, expectedYear + " / " + expectedWeekNumber);

            string actualExchangeSetType = batchDetailsResponse.attributes[4].value;

            if (exchangeSetType.Equals("AIO"))
            {
                Assert.Equals(actualExchangeSetType, "AIO");
            }
            else if (exchangeSetType.Equals("Update"))
            {
                Assert.Equals(actualExchangeSetType, "Update");
                string actualMediaType = batchDetailsResponse.attributes[5].value;
                Assert.Equals(actualMediaType, "Zip");

            }
        }
    }
}
