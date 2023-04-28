using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOValidFunctionalScenarios")]
    public class POSAIOValidFunctionalScenarios
    {
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        private string DownloadedFolderPath { get; set; }
        private string fssJwtToken;

        [OneTimeSetUp]
        public async Task Setup()
        {
            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidAIOProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
            await CommonHelper.RunWebJobAio();
        }

        [Test]
        public async Task WhenICallBatchDetailsEndpointWithValidAioBatchId_ThenBatchDetailsShouldBeCorrect()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.AIOValidBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();

            GetBatchDetails.GetBatchDetailsResponseValidationForAio(batchDetailsResponse);
        }

        [Test]
        public async Task WhenIDownloadAioExchangeSet_ThenAdditionalAioCdFilesAreGenerated()
        {
            DownloadedFolderPath = await FileContentHelper.DownloadAndExtractAioZip(fssJwtToken, posDetails.AioExchangeSetBatchId);

            int fileCount = Directory.GetFiles(Path.Combine(DownloadedFolderPath, posDetails.AioFolderName,posDetails.InfoFolderName), "*.*", SearchOption.TopDirectoryOnly).Length;
            Assert.IsTrue(fileCount > 0, $"File count is {fileCount} in the specified folder path.");
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
