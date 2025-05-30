﻿using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("POSAIOEndToEndScenarioUpdatePollingTimeOut")]
    public class POSAIOEndToEndScenarioUpdatePollingTimeOut: ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            FssJwtToken = await AuthTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFMAio(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationUpdatePollingTimeout);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
            await CommonHelper.RunWebJobAio();
        }

        [Test]
        public async Task WhenExecutedAioWebJobForUpdateTimesOut_ThenCommitInProgressBatchStatusIsReturned()
        {
            HttpResponseMessage apiResponse = await GetBatchDetails.GetBatchDetailsEndpoint(FSSAuth.BaseUrl, posDetails.UpdatePollingTimeoutBatchId);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));

            dynamic batchDetailsResponse = await apiResponse.DeserializeAsyncResponse();
            string batchStatus = batchDetailsResponse.status;
            Assert.That(batchStatus, Is.EqualTo("CommitInProgress"));

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
