using System.Net;
using FluentAssertions;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOScenarioPollingTimeOut")]
    public class POSAIOScenarioPollingTimeOut
    {
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;

        [OneTimeSetUp]
        public async Task Setup()
        {
            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationFullAvcsPollingTimeOut);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenTheExecutedPosWebJobForAioTimesOut_ThenCommitInProgressBatchStatusIsReturned()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.FullAvcsPollingTimeOutBatchId);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();
            string batchStatus = batchDetailsResponse.status;
            batchStatus.Should().Be("CommitInProgress");
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
