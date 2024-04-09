using Azure.Storage.Queues;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BuilderService
{
    [TestFixture]
    public class RequestAndDownloadExchangeSet 
    {
        static readonly TestConfiguration testConfiguration = new();

        [Test]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s57", "CHANGE")]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s63", "CHANGE")]
        [TestCase("9f349e4f-a4f0-45e8-9a3a-7baa3c561c7e", "s57", "BASE")]
        [TestCase("0d91fb1a-cbe2-4443-8f61-e9a925fa00c9", "s63", "BASE")]
        [TestCase("27067a02-df4b-49a1-8699-442b265a75d2", "s57", "UPDATE")]
        [TestCase("27067a02-df4b-49a1-8699-442b265a75d2", "s63", "UPDATE")]
        public async Task WhenICheckDownloadedZipForTypesInConfigFile_ThenZipIsCreatedForRequestedProduct(string batchId, string exchangeSetStandard, string Type)
        {
            var queueMessage = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText("./TestData/BSQueueMessage.txt")) ;
            queueMessage.Type = Type;
            string jsonString = JsonConvert.SerializeObject(queueMessage);

            QueueClientOptions queueOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };
            QueueClient queue = new QueueClient(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName, queueOptions);
            queue.SendMessage(jsonString);

            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            bool expectedResulted = FssBatchHelper.CheckFilesInDownloadedZip(downloadFolderPath, exchangeSetStandard);
            expectedResulted.Should().Be(true);
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }
    }
}
