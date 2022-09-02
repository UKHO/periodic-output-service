
using NUnit.Framework;
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
            Assert.That(batchStatus, Is.EqualTo("Committed"), $"Expected Batch Status of Committed, but actual value is {batchStatus}");

            string businessUnit = batchDetailsResponse.businessUnit;
            Assert.That(businessUnit, Is.EqualTo("AVCSData"), $"Expected Business Unit of AVCSData, but actual value is {businessUnit}");

            string expiryDate = batchDetailsResponse.expiryDate;
            Assert.That(expiryDate.Contains(expectedExpiryDate), $"Expected Expiry Date to contain {expectedExpiryDate}, but actual value is {expiryDate}");
        }

        public static void GetBatchDetailsResponseValidationForFullAVCSExchangeSet(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[6].value;
            if (mediaType.Equals("Zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals($"{posDetails.UpdateExchangeSet}"))
                {
                    Assert.That(fileName, Is.EqualTo($"{posDetails.UpdateExchangeSet}"), $"Expected Response File Name Zip of {posDetails.UpdateExchangeSet}, but actual value is {fileName}");
                }
                else
                {
                    int responseFileNameContent = 0;

                    for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
                    {
                        var folderName = $"M0{mediaNumber}X02.zip";
                        string responseFileNameZip = batchDetailsResponse.files[responseFileNameContent].filename;
                        Assert.That(responseFileNameZip, Is.EqualTo(folderName), $"Expected Response File Name Zip of {folderName}, but actual value is {responseFileNameZip}");

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
                    Assert.That(responseFileName, Is.EqualTo(expectedFileName[responseFileNameLocation]), $"Expected Response File Name of {expectedFileName[responseFileNameLocation]}, but actual value is {responseFileName}");
                }
            }
            else
            {
                Assert.Fail($"{mediaType} is different than Zip & DVD");
            }
        }

        public static void GetBatchDetailsResponseValidationForCatalogueXmlOrEncUpdateListCsv(dynamic batchDetailsResponse)
        {
            string responseContent = batchDetailsResponse.attributes[5].value;
            if (responseContent.Equals("Catalogue"))
            {
                string responseFileName = batchDetailsResponse.files[0].filename;
                Assert.That(responseFileName, Is.EqualTo(posDetails.AVCSCatalogueFileName), $"Expected Response File Name of {posDetails.AVCSCatalogueFileName}, but actual value is {responseFileName}");
            }
            else if (responseContent.Equals("ENC Updates"))
            {
                string responseFileName = batchDetailsResponse.files[0].filename;
                Assert.That(responseFileName, Is.EqualTo(posDetails.EncUpdateListFileName), $"Expected Response File Name of {posDetails.EncUpdateListFileName}, but actual value is {responseFileName}");
            }
            else
            {
                Assert.Fail($"{responseContent} is different than Catalogue.xml or Enc Updates list.csv");
            }
        }
    }
}
