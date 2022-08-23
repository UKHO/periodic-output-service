
using NUnit.Framework;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        static readonly HttpClient httpClient = new HttpClient();

        public static async Task<HttpResponseMessage> GetBatchDetailsEndpoint(string baseUrl, string BatchId)
        {
            string uri = $"{baseUrl}/batch/{BatchId}";

            var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri);

            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        public static void GetBatchDetailsResponseValidation(dynamic BatchDetailsResponse)
        {
            string ExpectedExpiryDate = DateTime.UtcNow.Date.AddDays(28).ToString("MM/dd/yyyy");
            //to check status
            string BatchStatus = BatchDetailsResponse.status;
            Assert.That(BatchStatus, Is.EqualTo("Committed"), $"Expected Batch Status of Committed, but actual value is {BatchStatus}");

            string BusinessUnit = BatchDetailsResponse.businessUnit;
            Assert.That(BusinessUnit, Is.EqualTo("AVCSData"), $"Expected Business Unit of AVCSData, but actual value is {BusinessUnit}");

            string ExpiryDate = BatchDetailsResponse.expiryDate;
            Assert.That(ExpiryDate.Contains(ExpectedExpiryDate), $"Expected Expiry Date to contain {ExpectedExpiryDate}, but actual value is {ExpiryDate}");
        }

        public static void GetBatchDetailsResponseValidationForIsoAndSha1Files(dynamic BatchDetailsResponse)
        {
            string MediaType = BatchDetailsResponse.attributes[1].value;
            Assert.That(MediaType, Is.EqualTo("DVD"), $"Expected Media Type of DVD, but actual value is {MediaType}");

            string[] ExpectedFileName = { "M01X02.iso", "M01X02.iso.sha1", "M02X02.iso", "M02X02.iso.sha1" };
            for (int ResponseFileNameLocation = 0; ResponseFileNameLocation < ExpectedFileName.Length; ResponseFileNameLocation++)
            {
                string ResponseFileName = BatchDetailsResponse.files[ResponseFileNameLocation].filename;
                Assert.That(ResponseFileName, Is.EqualTo(ExpectedFileName[ResponseFileNameLocation]), $"Expected Response File Name of {ExpectedFileName[ResponseFileNameLocation]}, but actual value is {ResponseFileName}");
            }
        }

        public static void GetBatchDetailsResponseValidationForZipFiles(dynamic BatchDetailsResponse)
        {
            string MediaType = BatchDetailsResponse.attributes[1].value;
            Assert.That(MediaType, Is.EqualTo("Zip"), $"Expected Media Type of Zip, but actual value is {MediaType}");
            int ResponseFileNameContent = 0;

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {                
                var folderName = $"M0{mediaNumber}X02.zip";
                string responseFileNameZip = BatchDetailsResponse.files[ResponseFileNameContent].filename;
                Assert.That(responseFileNameZip, Is.EqualTo(folderName), $"Expected Response File Name Zip of {folderName}, but actual value is {responseFileNameZip}");

                ResponseFileNameContent++;
            }
        }
    }
}
