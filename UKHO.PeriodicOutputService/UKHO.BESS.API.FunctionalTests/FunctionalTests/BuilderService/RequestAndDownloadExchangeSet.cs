using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BuilderService
{
    [TestFixture]
    public class RequestAndDownloadExchangeSet
    {
        static readonly TestConfiguration testConfiguration = new();

        [OneTimeSetUp]
        public void SetupAsync()
        {
            HttpResponseMessage apiResponse = Extensions.ConfigureFt(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.Identifiers);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        //PBI 150897: Testing : BESS BS - Request, wait/poll and download exchange set
        [Test]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s57", "CHANGE")]
        [TestCase("0f13a253-db5d-4b77-a165-643f4b4a77fc", "s63", "CHANGE")]
        [TestCase("f8fd2fb4-3dd6-425d-b34f-3059e262feed", "s57", "BASE")]
        [TestCase("4bc70797-7ee6-407f-bafe-cae49a5b5f91", "s63", "BASE")]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s57", "UPDATE")]
        [TestCase("0f13a253-db5d-4b77-a165-643f4b4a77fc", "s63", "UPDATE")]
        public async Task WhenIAddAQueueMessageWithCorrectDetails_ThenExchangeSetShouldBeCreatedForRequestedProduct(string batchId, string exchangeSetStandard, string type)
        {
            Extensions.AddQueueMessage(type, exchangeSetStandard, testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName);
            Extensions.WaitForDownloadExchangeSet();
            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            bool expectedResulted = FssBatchHelper.CheckFilesInDownloadedZip(downloadFolderPath, exchangeSetStandard);
            expectedResulted.Should().Be(true);
        }

        [TearDown]
        public async Task TearDown()
        {
            //cleaning bessproductversiondetails azure table entries
            await Extensions.DeleteTableEntries(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.TableName, testConfiguration.bessConfig.ProductsName);

            //cleaning up the downloaded files from temp folder
            Extensions.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = Extensions.Cleanup(testConfiguration.bessConfig.BaseUrl);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
    }
}