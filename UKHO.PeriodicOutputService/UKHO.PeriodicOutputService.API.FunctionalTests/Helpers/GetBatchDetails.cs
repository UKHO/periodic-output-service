
using System.Globalization;
using FluentAssertions;
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
            batchStatus.Should().Be("Committed");

            string businessUnit = batchDetailsResponse.businessUnit;
            businessUnit.Should().Be("AVCSData");

            string expiryDate = batchDetailsResponse.expiryDate;
            expiryDate.Should().Contain(expectedExpiryDate);

            string fileSize = batchDetailsResponse.files[0].fileSize;
            fileSize.Should().NotBeNullOrEmpty();

            string hash = batchDetailsResponse.files[0].hash;
            hash.Should().NotBeNullOrEmpty();
        }

        public static void GetBatchDetailsResponseValidationForFullAVCSExchangeSet(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[5].value;
            if (mediaType.ToLower().Equals("zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Contains("UPDATE"))
                {
                    fileName.Should().Be(string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear));

                    string responseFileMimeName = batchDetailsResponse.files[0].mimeType;
                    responseFileMimeName.Should().Be(posDetails.ZipFileMimeType);
                }
                else
                {
                    int responseFileNameContent = 0;
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        string folderName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        responseFileNameZip.Should().Be(folderName);

                        string responseFileMimeName = batchDetailsResponse.files[responseFileNameContent].mimeType;
                        responseFileMimeName.Should().Be(posDetails.ZipFileMimeType);

                        string fileSize = batchDetailsResponse.files[responseFileNameContent].fileSize;
                        fileSize.Should().NotBeNullOrEmpty();

                        string hash = batchDetailsResponse.files[responseFileNameContent].hash;
                        hash.Should().NotBeNullOrEmpty();

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
                    responseFileName.Should().Be(expectedFileName[responseFileNameLocation]);

                    string responseFileMimeName = batchDetailsResponse.files[responseFileNameLocation].mimeType;

                    responseFileMimeName.Should().Be(responseFileName.Contains(".sha1")
                        ? posDetails.Sha1FileMimeType
                        : posDetails.IsoFileMimeType);

                    string fileSize = batchDetailsResponse.files[responseFileNameLocation].fileSize;
                    fileSize.Should().NotBeNullOrEmpty();

                    string hash = batchDetailsResponse.files[responseFileNameLocation].hash;
                    hash.Should().NotBeNullOrEmpty();
                }
            }
            else
            {
                mediaType.Should().ContainAny("Zip", "DVD");
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
                    responseFileName.Should().Be(posDetails.AVCSCatalogueFileName);
                    responseFileMimeName.Should().Be(posDetails.AVCSCatalogueFileMimeType);
                    break;
                case "ENC Updates":
                    responseFileName.Should().Be(posDetails.EncUpdateListFileName);
                    responseFileMimeName.Should().Be(posDetails.EncUpdateListFileMimeType);
                    break;
                default:
                    responseContent.Should().ContainAny("Catalogue.xml", "Enc Update list.csv");
                    break;
            }
        }

        public static void GetBatchDetailsResponseValidationForAio(dynamic batchDetailsResponse)
        {
            string expectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");
            string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
            string yearNumber = DateTime.UtcNow.Year.ToString();
            string year2 = DateTime.UtcNow.Year.ToString().Substring(DateTime.UtcNow.Year.ToString().Length - 2);

            //to check status
            string batchStatus = batchDetailsResponse.status;
            batchStatus.Should().Be("Committed");

            string businessUnit = batchDetailsResponse.businessUnit;
            businessUnit.Should().Be("AVCSData");

            string expiryDate = batchDetailsResponse.expiryDate;
            expiryDate.Should().Contain(expectedExpiryDate);

            //string fileSize = batchDetailsResponse.files[0].fileSize;
            //fileSize.Should().NotBeNullOrEmpty();

            //string hash = batchDetailsResponse.files[0].hash;
            //hash.Should().NotBeNullOrEmpty();

            //string mimeType = batchDetailsResponse.files[0].mimeType;
            //mimeType.Should().Be("application/x-raw-disk-image");

            string productType = batchDetailsResponse.attributes[0].value;
            productType.Should().Be("AIO");

            string weeknumber = batchDetailsResponse.attributes[1].value;
            weeknumber.Should().Be(weekNumber);

            string year = batchDetailsResponse.attributes[2].value;
            year.Should().Be(yearNumber);

            string yearweek = batchDetailsResponse.attributes[3].value;
            yearweek.Should().Be(yearNumber + " / " +weeknumber);

            string exchangesettype = batchDetailsResponse.attributes[4].value;
            exchangesettype.Should().Be("AIO");
        }
    }
}
