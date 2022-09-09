using System.Net;
using FluentAssertions;
using NUnit.Framework;
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
        private static readonly FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        private HttpResponseMessage POSWebJobApiResponse;
        private List<string> DownloadedFolderPath;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            fssJwtToken = await authTokenProvider.GetFssToken();
            WebJob = new POSWebJob();
            if (!posWebJob.IsRunningOnLocalMachine)
            {
                string POSWebJobuserCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(posWebJob.UserName, posWebJob.Password);
                POSWebJobApiResponse = await WebJob.POSWebJobEndPoint(posWebJob.BaseUrl, POSWebJobuserCredentialsBytes);
                POSWebJobApiResponse.StatusCode.Should().Be((HttpStatusCode)202);

                //As there is no way to check if webjob execution is completed, we have added below delay to wait to get webjob execution completes. 
                await Task.Delay(120000);
            }
        }

        [Test]
        [TestCase("f9523d33-ef12-4cc1-969d-8a95f094a48b", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch1IsCreatedAndUploadedForISOSha1Files")]
        [TestCase("483aa1b9-8a3b-49f2-bae9-759bb93b04d1", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch2IsCreatedAndUploadedForZipFiles")]
        public async Task WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatchesAreCreatedAndUploadedForLargeMedia(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, batchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenZipFilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.ZipFilesBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.ZipFilesBatchId, fssJwtToken, batchDetailsResponse);
            DownloadedFolderPath.Count.Should().Be(2);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenIsoAndSha1FilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.IsoSha1BatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForIsoAndSha1Files(posDetails.IsoSha1BatchId, fssJwtToken);
            DownloadedFolderPath.Count.Should().Be(4);
        }

        [Test]
        public async Task WhenDownloadedZipOfUpdateExchangeSetFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateExchangeSetBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForUpdateExchangeSetWithBatchId_ThenAZipFileIsDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateExchangeSetBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.UpdateExchangeSetBatchId, fssJwtToken, batchDetailsResponse);
            DownloadedFolderPath.Count.Should().Be(1);
        }

        [Test]
        [TestCase("bece0a26-867c-4ea6-8ece-98afa246a00e", TestName = "WhenDownloadedCatalogueXmlFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU")]
        [TestCase("472b599d-494d-4ac5-a281-91e3927b24d4", TestName = "WhenDownloadedEncUpdatesListCsvFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU")]

        public async Task WhenDownloadedCatalogueXmlAndCsvFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.CatalogueBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForCatalogueXmlOrEncUpdateListCsv(batchDetailsResponse);
        }

        [Test]
        [TestCase("bece0a26-867c-4ea6-8ece-98afa246a00e", TestName = "WhenICallFileDownloadEndpointForCatalogueXmlWithBatchId_ThenAXmlFileIsDownloaded")]
        [TestCase("472b599d-494d-4ac5-a281-91e3927b24d4", TestName = "WhenICallFileDownloadEndpointForEncUpdatesListCsvWithBatchId_ThenACsvFileIsDownloaded")]

        public async Task WhenICallFileDownloadEndpointForCatalogueXmlWithBatchId_ThenAXmlAndCSVFilesAreDownloaded1(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, batchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadCatalogueXmlOrEncUpdatesListCsvFileForLargeMedia(batchId, fssJwtToken, batchDetailsResponse);
            DownloadedFolderPath.Count.Should().Be(1);
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = MockHelper.Cleanup(FSSAuth.BaseUrl);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
    }
}
