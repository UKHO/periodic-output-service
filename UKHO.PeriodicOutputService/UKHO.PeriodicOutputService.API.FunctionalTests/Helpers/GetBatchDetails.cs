
using NUnit.Framework;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class GetBatchDetails
    {
        static HttpClient httpClient = new HttpClient();

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
            Assert.That(BatchStatus, Is.EqualTo("Committed"), $"Value is not matching, Actual value is : {BatchStatus}");

            string BusinessUnit = BatchDetailsResponse.businessUnit;
            Assert.That(BusinessUnit, Is.EqualTo("AVCSData"), $"Value is not matching, Actual value is : {BusinessUnit}");

            string ExpiryDate = BatchDetailsResponse.expiryDate;
            Assert.That(ExpiryDate.Contains(ExpectedExpiryDate), $"Value is not matching, Actual value is : {ExpiryDate}");
        }

        public static void GetBatchDetailsResponseValidationForIsoAndSha1Files(dynamic BatchDetailsResponse)
        {
            string MediaType = BatchDetailsResponse.attributes[1].value;
            Assert.That(MediaType, Is.EqualTo("DVD"), $"Value is not matching, Actual value is : {MediaType}");

            string[] ExpectedFileName = { "M01X02.iso", "M01X02.iso.sha1", "M02X02.iso", "M02X02.iso.sha1" };
            for (int ResponseFileNameLocation = 0; ResponseFileNameLocation < ExpectedFileName.Length; ResponseFileNameLocation++)
            {
                string ResponseFileName = BatchDetailsResponse.files[ResponseFileNameLocation].filename;
                Assert.That(ResponseFileName, Is.EqualTo(ExpectedFileName[ResponseFileNameLocation]), $"Value is not matching, Actual value is : {ResponseFileName}");
            }
        }

        public static void GetBatchDetailsResponseValidationForZipFiles(dynamic BatchDetailsResponse)
        {
            string MediaType = BatchDetailsResponse.attributes[1].value;
            Assert.That(MediaType, Is.EqualTo("Zip"), $"Value is not matching, Actual value is : {MediaType}");
            int ResponseFileNameContent = 0;

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {                
                var FolderName = $"M0{mediaNumber}X02.zip";
                string ResponseFileNameZip = BatchDetailsResponse.files[ResponseFileNameContent].filename;
                Assert.That(ResponseFileNameZip, Is.EqualTo(FolderName), $"Value is not matching, Actual value is : {ResponseFileNameZip}");

                ResponseFileNameContent++;
            }
        }
    }
}
