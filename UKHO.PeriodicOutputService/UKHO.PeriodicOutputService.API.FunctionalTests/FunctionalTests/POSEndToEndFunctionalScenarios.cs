using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class POSEndToEndFunctionalScenarios
    {
        private TestConfiguration config;
        private string FssJwtToken;
        public string userCredentialsBytes;
        private POSWebJob WebJob;
        private static readonly POSWebjobApiConfiguration POSWebJob = new TestConfiguration().POSWebJobConfig;

        static FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        HttpResponseMessage POSWebJobApiResponse;
        private List<string> DownloadedFolderPath;
        public string IsoSha1BatchId = "f9523d33-ef12-4cc1-969d-8a95f094a48b";
        public string ZipFilesBatchId = "483aa1b9-8a3b-49f2-bae9-759bb93b04d1";

        [OneTimeSetUp]
        public async Task Setup()
        {
            WebJob = new POSWebJob();
            config = new TestConfiguration();
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();
            string POSWebJobuserCredentialsBytes = CommonHelper.getbase64encodedcredentials(POSWebJob.userName, POSWebJob.password);
            POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(POSWebJob.baseUrl, POSWebJobuserCredentialsBytes);
            await Task.Delay(120000); //As this functionality is related to a webjob not an endpoint , so this is required to complete the webjob execution and then proceed further.
        }

        [Test]
        public async Task WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch1IsCreatedAndUploadedForISOAndSha1Files()
        {
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");

            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, IsoSha1BatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var BatchDetailsResponse = await CommonHelper.DeserializeAsyncResponse(apiResponse);

            GetBatchDetails.GetBatchDetailsResponseValidation(BatchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForIsoAndSha1Files(BatchDetailsResponse);
        }

        [Test]
        public async Task WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch2IsCreatedAndUploadedForZipFiles()
        {
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");

            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, ZipFilesBatchId);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiResponse.StatusCode}, instead of the expected status 200.");

            var BatchDetailsResponse = await CommonHelper.DeserializeAsyncResponse(apiResponse);

            GetBatchDetails.GetBatchDetailsResponseValidation(BatchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForZipFiles(BatchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithValidBatchId_ThenALargeMediaZipFilesAreGenerated()
        {
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(ZipFilesBatchId, FssJwtToken);
            Assert.That((int)DownloadedFolderPath.Count, Is.EqualTo(2), $"DownloadFolderCount : {(int)DownloadedFolderPath.Count} is incorrect");
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithValidBatchId_ThenALargeMediaIsoAndSha1FilesAreGenerated()
        {
            Assert.That((int)POSWebJobApiResponse.StatusCode, Is.EqualTo(202), $"Incorrect status code is returned {POSWebJobApiResponse.StatusCode}, instead of the expected status 202.");

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForIsoAndSha1Files(IsoSha1BatchId, FssJwtToken);
            Assert.That((int)DownloadedFolderPath.Count, Is.EqualTo(4), $"DownloadFolderCount : {(int)DownloadedFolderPath.Count} is incorrect");
        }

        [OneTimeTearDown]
        public void globalteardown()
        {
            //clean up downloaded files/folders
            for (int medianumber = 1; medianumber <= 2; medianumber++)
            {
                var foldername = $"m0{medianumber}x02";
                FileContentHelper.DeleteDirectoryForIsoAndSha1Files(foldername + ".zip");
                FileContentHelper.DeleteDirectoryForIsoAndSha1Files(foldername + ".iso");
                FileContentHelper.DeleteDirectoryForIsoAndSha1Files(foldername + ".iso.sha1");
            }
        }
    }
}
