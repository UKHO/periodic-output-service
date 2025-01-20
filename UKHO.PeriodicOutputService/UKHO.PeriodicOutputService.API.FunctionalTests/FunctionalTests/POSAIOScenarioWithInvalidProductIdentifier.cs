using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOScenarioWithInvalidProductIdentifier")]
    public class POSAIOScenarioWithInvalidProductIdentifier :  ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationInValidProductIdentifier);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
            await CommonHelper.RunWebJobAio();
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithInvalidAioProductIdentifier_ThenABatchWithErrorTxtIsGenerated()
        {
            HttpResponseMessage responseMessage = await FssBatchHelper.VerifyErrorTxtExist(FssJwtToken);
            Assert.Equals(responseMessage.StatusCode, (HttpStatusCode)200);
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
