using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSEndToEndValidFunctionalScenarios")]
    public class POSEndToEndValidFunctionalScenarios: ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();
            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidProductIdentifier);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
            await CommonHelper.RunWebJob();
        }

        [Test]
        [TestCase("f9523d33-ef12-4cc1-969d-8a95f094a48b", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch1IsCreatedAndUploadedForISOSha1Files")]
        [TestCase("483aa1b9-8a3b-49f2-bae9-759bb93b04d1", TestName = "WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatch2IsCreatedAndUploadedForZipFiles")]
        public async Task WhenExtractedZipAndGeneratedISOAndSha1Files_ThenBatchesAreCreatedAndUploadedForLargeMedia(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, batchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenZipFilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.ZipFilesBatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.ZipFilesBatchId, FssJwtToken, batchDetailsResponse);
            Assert.Equals(DownloadedFolderPath.Count, 2);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForFullAVCSExchangeSetWithBatchId_ThenIsoAndSha1FilesAreDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.IsoSha1BatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForIsoAndSha1Files(posDetails.IsoSha1BatchId, FssJwtToken);
            Assert.Equals(DownloadedFolderPath.Count, 4);
        }

        [Test]
        public async Task WhenDownloadedZipOfUpdateExchangeSetFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateExchangeSetBatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidation(batchDetailsResponse);

            GetBatchDetails.GetBatchDetailsResponseValidationForFullAVCSExchangeSet(batchDetailsResponse);
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointForUpdateExchangeSetWithBatchId_ThenAZipFileIsDownloaded()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdateExchangeSetBatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractExchangeSetZipFileForLargeMedia(posDetails.UpdateExchangeSetBatchId, FssJwtToken, batchDetailsResponse);
            Assert.Equals(DownloadedFolderPath.Count, 1);
        }

        [Test]
        [TestCase("bece0a26-867c-4ea6-8ece-98afa246a00e", TestName = "WhenDownloadedCatalogueXmlFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU")]
        [TestCase("472b599d-494d-4ac5-a281-91e3927b24d4", TestName = "WhenDownloadedEncUpdatesListCsvFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU")]

        public async Task WhenDownloadedCatalogueXmlAndCsvFromFss_ThenNewBatchIsCreatedAndUploadedWithUpdatedBU(string batchId)
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.CatalogueBatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

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
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            DownloadedFolderPath = await FileContentHelper.DownloadCatalogueXmlOrEncUpdatesListCsvFileForLargeMedia(batchId, FssJwtToken, batchDetailsResponse);
            Assert.Equals(DownloadedFolderPath.Count, 1);
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }
    }
}
