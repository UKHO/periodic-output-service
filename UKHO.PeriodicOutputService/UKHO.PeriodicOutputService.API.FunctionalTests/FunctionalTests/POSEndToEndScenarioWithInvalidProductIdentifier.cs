using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

[assembly: Parallelizable(ParallelScope.Fixtures)]
[assembly: LevelOfParallelism(4)]
namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSEndToEndScenarioWithInvalidProductIdentifier")]
    public class POSEndToEndScenarioWithInvalidProductIdentifier : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationInValidProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenICallFileDownloadEndpointWithInvalidProductIdentifier_ThenABatchWithErrorTxtIsGenerated()
        {
            HttpResponseMessage responseMessage = await FssBatchHelper.VerifyErrorTxtExist(FssJwtToken);
            responseMessage.StatusCode.Should().Be((HttpStatusCode)200);

            await FileContentHelper.VerifyPosBatches(FssJwtToken);
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
