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
        [TestCase("0f13a253-db5d-4b77-a165-643f4b4a77fc", "s63", "CHANGE")]
        [TestCase("f8fd2fb4-3dd6-425d-b34f-3059e262feed", "s57", "BASE")]
        [TestCase("4bc70797-7ee6-407f-bafe-cae49a5b5f91", "s63", "BASE")]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s57", "UPDATE")]
        [TestCase("0f13a253-db5d-4b77-a165-643f4b4a77fc", "s63", "UPDATE")]
        public async Task WhenICheckDownloadedZipForTypesInConfigFile_ThenZipIsCreatedForRequestedProduct(string batchId, string exchangeSetStandard, string Type)
        {
            var queueMessage = JsonConvert.DeserializeObject<ConfigQueueMessage>(File.ReadAllText("./TestData/BSQueueMessage.txt")) ;
            queueMessage!.Type = Type;
            queueMessage.ExchangeSetStandard = exchangeSetStandard;
            string jsonString = JsonConvert.SerializeObject(queueMessage);

            QueueClientOptions queueOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };
            QueueClient queue = new QueueClient(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName, queueOptions);
            queue.SendMessage(jsonString);

            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            bool expectedResulted = FssBatchHelper.CheckFilesInDownloadedZip(downloadFolderPath, exchangeSetStandard);
            expectedResulted.Should().Be(true);
        }

        [TearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }
    }
}
