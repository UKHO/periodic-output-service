using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSEndToEndScenarioUpdatePollingTimeOut")]
    public class POSEndToEndScenarioUpdatePollingTimeOut: ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            AuthTokenProvider authTokenProvider = new();
            FssJwtToken = await authTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationUpdatePollingTimeout);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenTheExecutedPosWebJobForUpdateTimesOut_ThenCommitInProgressBatchStatusIsReturned()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdatePollingTimeoutBatchId);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();
            string batchStatus = batchDetailsResponse.status;
            Assert.Equals(batchStatus, "CommitInProgress");

            await FileContentHelper.VerifyPosBatches(FssJwtToken);
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
