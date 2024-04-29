using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOValidFunctionalScenarios")]
    public class POSAIOValidFunctionalScenarios : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidAIOProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
            await CommonHelper.RunWebJobAio();
        }

        [Test]
        public async Task WhenICallBatchDetailsEndpointForFullExchangeSetTypeWithValidAioBatchId_ThenBatchDetailsShouldBeCorrect()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.AIOFullValidBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidationForAio(batchDetailsResponse, "AIO");
        }

        [Test]
        public async Task WhenIDownloadAioExchangeSet_ThenAdditionalAioCdFilesAreGenerated()
        {
            string DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(FssJwtToken, posDetails.AioExchangeSetBatchId);

            int fileCount = Directory.GetFiles(Path.Combine(DownloadedFolderPath, posDetails.AioFolderName,posDetails.InfoFolderName), "*.*", SearchOption.TopDirectoryOnly).Length;
            Assert.That(fileCount > 0, $"File count is {fileCount} in the specified folder path.");
        }

        [Ignore("Temp.")]
        [Test]
        public async Task WhenICallBatchDetailsEndpointForUpdateExchangeSetTypeWithValidAioBatchId_ThenBatchDetailsShouldBeCorrect()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.AIOUpdateValidBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidationForAio(batchDetailsResponse, "Update");
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
    }
}
