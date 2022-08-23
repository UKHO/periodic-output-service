
using NUnit.Framework;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
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

        public static void GetBatchDetailsResponseValidationForIsoAndSha1Files(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[1].value;
            Assert.That(mediaType, Is.EqualTo("DVD"), $"Expected Media Type of DVD, but actual value is {mediaType}");

            string[] expectedFileName = { "M01X02.iso", "M01X02.iso.sha1", "M02X02.iso", "M02X02.iso.sha1" };
            for (int responseFileNameLocation = 0; responseFileNameLocation < expectedFileName.Length; responseFileNameLocation++)
            {
                string responseFileName = batchDetailsResponse.files[responseFileNameLocation].filename;
                Assert.That(responseFileName, Is.EqualTo(expectedFileName[responseFileNameLocation]), $"Expected Response File Name of {expectedFileName[responseFileNameLocation]}, but actual value is {responseFileName}");
            }
        }

        public static void GetBatchDetailsResponseValidationForZipFiles(dynamic batchDetailsResponse)
        {
            string mediaType = batchDetailsResponse.attributes[1].value;
            Assert.That(mediaType, Is.EqualTo("Zip"), $"Expected Media Type of Zip, but actual value is {mediaType}");
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
}
