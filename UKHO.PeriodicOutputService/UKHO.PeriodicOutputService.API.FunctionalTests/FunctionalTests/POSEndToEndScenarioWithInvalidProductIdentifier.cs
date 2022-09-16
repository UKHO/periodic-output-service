using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [NonParallelizable]
    public class POSEndToEndScenarioWithInvalidProductIdentifier
    {
        private string fssJwtToken;
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;

        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            fssJwtToken = await authTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationInValidProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithInvalidProductIdentifier_ThenABatchWithErrorTxtIsGenerated()
        {
            HttpResponseMessage responseMessage = await FssBatchHelper.VerifyErrorTxtExist(fssJwtToken);
            responseMessage.StatusCode.Should().Be((HttpStatusCode)200);

            await FileContentHelper.VerifyPosBatches(fssJwtToken);
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
