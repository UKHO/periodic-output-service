
using NUnit.Framework;
using FluentAssertions;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        static readonly HttpClient httpClient = new HttpClient();

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
                if (fileName.Equals($"{posDetails.UpdateExchangeSet}"))
                {
                    fileName.Should().Be($"{posDetails.UpdateExchangeSet}");
                }
                else
                {
                    int responseFileNameContent = 0;

                    for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
                    {
                        var folderName = $"M0{mediaNumber}X02.zip";
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        responseFileNameZip.Should().Be(folderName);
                        responseFileNameContent++;
                    }

                }
            }
            else if (mediaType.Equals("DVD"))
            {
                string[] expectedFileName = { posDetails.M01IsoFile, posDetails.M01Sha1File, posDetails.M02IsoFile, posDetails.M02Sha1File };
                for (int responseFileNameLocation = 0; responseFileNameLocation < expectedFileName.Length; responseFileNameLocation++)
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
            if (responseContent.Equals("Catalogue"))
            {
                string responseFileName = batchDetailsResponse.files[0].filename;
                responseFileName.Should().Be(posDetails.AVCSCatalogueFileName);
            }
            else if (responseContent.Equals("ENC Updates"))
            {
                string responseFileName = batchDetailsResponse.files[0].filename;
                responseFileName.Should().Be(posDetails.EncUpdateListFileName);
            }
            else
            {
                responseContent.Should().ContainAny("Catalogue.xml", "Enc Update list.csv");
            }
        }
    }
}
