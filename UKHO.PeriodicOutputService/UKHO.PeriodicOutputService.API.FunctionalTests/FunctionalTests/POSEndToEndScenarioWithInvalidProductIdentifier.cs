using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSEndToEndScenarioWithInvalidProductIdentifier")]
    public class POSEndToEndScenarioWithInvalidProductIdentifier : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            FssJwtToken = await AuthTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationInValidProductIdentifier);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithInvalidProductIdentifier_ThenABatchWithErrorTxtIsGenerated()
        {
            HttpResponseMessage responseMessage = await FssBatchHelper.VerifyErrorTxtExist(FssJwtToken);
            Assert.That(responseMessage.StatusCode, Is.EqualTo((HttpStatusCode)200));

            await FileContentHelper.VerifyPosBatches(FssJwtToken);
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
        }
    }
}
