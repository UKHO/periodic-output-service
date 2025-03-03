using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly HttpClient s_httpClient = new();
        private static readonly string s_weekNumber;
        private static readonly string s_currentYearShort;
        private static readonly string s_weekNumberAio;
        private static readonly string s_currentYearAio;
        private static readonly List<string> expectedFileName = [];

        static GetBatchDetails()
        {
            (s_weekNumber, _, s_currentYearShort) = CommonHelper.GetCurrentWeekAndYear();
            (s_weekNumberAio, s_currentYearAio, _) = CommonHelper.GetCurrentWeekAndYearAio();
        }

        public static async Task<HttpResponseMessage> GetBatchDetailsEndpoint(string baseUrl, string batchId)
        {
            string uri = $"{baseUrl}/batch/{batchId}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            return await s_httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public static void GetBatchDetailsResponseValidation(dynamic batchDetailsResponse)
        {
            string expectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");
            //to check status
            string batchStatus = batchDetailsResponse.status;
            Assert.That(batchStatus, Is.EqualTo("Committed"));

            string businessUnit = batchDetailsResponse.businessUnit;
            Assert.That(businessUnit, Is.EqualTo("AVCSData"));

            string expiryDate = batchDetailsResponse.expiryDate;
            Assert.That(expiryDate, Does.Contain(expectedExpiryDate));

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
                    Assert.That(fileName, Is.EqualTo(string.Format(posDetails.PosUpdateZipFileName, s_weekNumber, s_currentYearShort)));

                    string responseFileMimeName = batchDetailsResponse.files[0].mimeType;
                    Assert.That(responseFileMimeName, Is.EqualTo(posDetails.ZipFileMimeType));
                }
                else
                {
                    int responseFileNameContent = 0;
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        string folderName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, s_weekNumber, s_currentYearShort);
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        Assert.That(responseFileNameZip, Is.EqualTo(folderName));

                        string responseFileMimeName = batchDetailsResponse.files[responseFileNameContent].mimeType;
                        Assert.That(responseFileMimeName, Is.EqualTo(posDetails.ZipFileMimeType));

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
                    string folderNameIso = string.Format(posDetails.PosAvcsIsoFileName, dvdNumber, s_weekNumber, s_currentYearShort);
                    string FolderNameSha1 = string.Format(posDetails.PosAvcsIsoSha1FileName, dvdNumber, s_weekNumber, s_currentYearShort);
                    expectedFileName.Add(folderNameIso);
                    expectedFileName.Add(FolderNameSha1);
                }

                for (int responseFileNameLocation = 0; responseFileNameLocation < expectedFileName.Count; responseFileNameLocation++)
                {
                    string responseFileName = batchDetailsResponse.files[responseFileNameLocation].filename;
                    Assert.That(responseFileName, Is.EqualTo(expectedFileName[responseFileNameLocation]));

                    string responseFileMimeName = batchDetailsResponse.files[responseFileNameLocation].mimeType;

                    Assert.That(responseFileMimeName, Is.EqualTo(responseFileName.Contains(".sha1")
                        ? posDetails.Sha1FileMimeType
                        : posDetails.IsoFileMimeType));

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
                    Assert.That(responseFileName, Is.EqualTo(posDetails.AVCSCatalogueFileName));
                    Assert.That(responseFileMimeName, Is.EqualTo(posDetails.AVCSCatalogueFileMimeType));
                    break;
                case "ENC Updates":
                    Assert.That(responseFileName, Is.EqualTo(posDetails.EncUpdateListFileName));
                    Assert.That(responseFileMimeName, Is.EqualTo(posDetails.EncUpdateListFileMimeType));
                    break;
                default:
                    Assert.That(responseContent.Contains("Catalogue.xml") || responseContent.Contains("Enc Update list.csv"));
                    break;
            }
        }

        public static void GetBatchDetailsResponseValidationForAio(dynamic batchDetailsResponse, string exchangeSetType)
        {
            var expectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");

            //to check status
            string actualBatchStatus = batchDetailsResponse.status;
            Assert.That(actualBatchStatus, Is.EqualTo("Committed"));

            string actualBusinessUnit = batchDetailsResponse.businessUnit;
            Assert.That(actualBusinessUnit, Is.EqualTo("AVCSData"));

            string actualExpiryDate = batchDetailsResponse.expiryDate;
            Assert.That(actualExpiryDate, Does.Contain(expectedExpiryDate));

            string actualProductType = batchDetailsResponse.attributes[0].value;
            Assert.That(actualProductType, Is.EqualTo("AIO"));

            string actualWeekNumber = batchDetailsResponse.attributes[1].value;
            Assert.That(actualWeekNumber, Is.EqualTo(s_weekNumberAio));

            string actualYear = batchDetailsResponse.attributes[2].value;
            Assert.That(actualYear, Is.EqualTo(s_currentYearAio));

            string actualYearAndWeek = batchDetailsResponse.attributes[3].value;
            Assert.That(actualYearAndWeek, Is.EqualTo(s_currentYearAio + " / " + s_weekNumberAio));

            string actualExchangeSetType = batchDetailsResponse.attributes[4].value;

            if (exchangeSetType.Equals("AIO"))
            {
                Assert.That(actualExchangeSetType, Is.EqualTo("AIO"));
            }
            else if (exchangeSetType.Equals("Update"))
            {
                Assert.That(actualExchangeSetType, Is.EqualTo("Update"));
                string actualMediaType = batchDetailsResponse.attributes[5].value;
                Assert.That(actualMediaType, Is.EqualTo("Zip"));
            }
        }
    }
}
