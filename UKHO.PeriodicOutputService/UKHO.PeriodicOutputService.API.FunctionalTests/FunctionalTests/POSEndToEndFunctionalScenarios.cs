using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class POSEndToEndFunctionalScenarios
    {
        private string fssJwtToken;
        private POSWebJob WebJob;
        private static readonly POSWebjobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;

        static FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        HttpResponseMessage POSWebJobApiResponse;
        private List<string> DownloadedFolderPath;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            fssJwtToken = await authTokenProvider.GetFssToken();
            WebJob = new POSWebJob();
            if (posDetails.IsRunningOnLocalMachine.Equals("false"))
            {
                string POSWebJobuserCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(posWebJob.UserName, posWebJob.Password);
                POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(posWebJob.BaseUrl, POSWebJobuserCredentialsBytes);
                Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");
            }
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

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenZipFilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.ZipFilesBatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.ZipFilesBatchId, fssJwtToken, batchDetailsResponse);
            Assert.That(DownloadedFolderPath.Count, Is.EqualTo(2), $"DownloadFolderCount : {DownloadedFolderPath.Count} is incorrect");
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenIsoAndSha1FilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.IsoSha1BatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForIsoAndSha1Files(posDetails.IsoSha1BatchId, fssJwtToken);
            Assert.That(DownloadedFolderPath.Count, Is.EqualTo(4), $"DownloadFolderCount : {DownloadedFolderPath.Count} is incorrect");
        }

        [Test]
        public async Task WhenDownloadedZipOfUpdateExchangeSetFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateSetBatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForUpdateExchangeSetWithBatchId_ThenAZipFileIsDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateSetBatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.UpdateSetBatchId, fssJwtToken, batchDetailsResponse);
            Assert.That(DownloadedFolderPath.Count, Is.EqualTo(1), $"DownloadFolderCount : {DownloadedFolderPath.Count} is incorrect");
        }

        [TearDown]
        public void GlobalTearDown()
        {
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);
        }
    }
}
