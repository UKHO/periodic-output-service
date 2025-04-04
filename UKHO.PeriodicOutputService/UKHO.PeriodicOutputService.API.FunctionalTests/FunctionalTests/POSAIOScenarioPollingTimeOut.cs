﻿using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOScenarioPollingTimeOut")]
    public class POSAIOScenarioPollingTimeOut : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationFullAvcsPollingTimeOut);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
            await CommonHelper.RunWebJobAio();
        }

        [Test]
        public async Task WhenTheExecutedPosWebJobForAioTimesOut_ThenCommitInProgressBatchStatusIsReturned()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.FullAvcsPollingTimeOutBatchId);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();
            string batchStatus = batchDetailsResponse.status;
            Assert.That(batchStatus, Is.EqualTo("CommitInProgress"));
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
