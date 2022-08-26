using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class POSEndToEndFunctionalScenarios
    {
        private string FssJwtToken;
        private POSWebJob WebJob;
        private static readonly POSWebjobApiConfiguration POSWebJob = new TestConfiguration().POSWebJobConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        private HttpResponseMessage POSWebJobApiResponse;
        private List<string> DownloadedFolderPath;

        [OneTimeSetUp]
        public async Task Setup()
        {
            WebJob = new POSWebJob();
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();
            string POSWebJobuserCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(POSWebJob.UserName, POSWebJob.Password);
            POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(POSWebJob.BaseUrl, POSWebJobuserCredentialsBytes);
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");
        }

        [Test]
        [TestCase("f9523d33-ef12-4cc1-969d-8a95f094a48b", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch1IsCreatedAndUploadedForISOSha1Files")]
        [TestCase("483aa1b9-8a3b-49f2-bae9-759bb93b04d1", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch2IsCreatedAndUploadedForZipFiles")]
        public async Task WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatchesAreCreatedAndUploadedForLargeMedia(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, batchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForISOSha1AndZipFiles(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithValidBatchId_ThenALargeMediaZipFilesAreGenerated()
        {
            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.ZipFilesBatchId, FssJwtToken);
            Assert.That(DownloadedFolderPath.Count, Is.EqualTo(2), $"DownloadFolderCount : {DownloadedFolderPath.Count} is incorrect");
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithValidBatchId_ThenALargeMediaIsoAndSha1FilesAreGenerated()
        {
            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForIsoAndSha1Files(posDetails.IsoSha1BatchId, FssJwtToken);
            Assert.That(DownloadedFolderPath.Count, Is.EqualTo(4), $"DownloadFolderCount : {DownloadedFolderPath.Count} is incorrect");
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //clean up downloaded files/folders
            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderName = $"M0{mediaNumber}X02";
                FileContentHelper.DeleteZipIsoSha1Files(folderName);
            }
        }
    }
}
