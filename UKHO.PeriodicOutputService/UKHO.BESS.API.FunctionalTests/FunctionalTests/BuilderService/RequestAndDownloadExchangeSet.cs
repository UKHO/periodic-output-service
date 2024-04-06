using System.Net;
using Azure.Storage.Queues;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BuilderService
{
    [TestFixture]
    public class RequestAndDownloadExchangeSet 
    {
        static readonly TestConfiguration testConfiguration = new();

        [Test]
        [TestCase("7b6edd6a-7a62-4271-a657-753f4c648531", "s57")]
        [TestCase("9f349e4f-a4f0-45e8-9a3a-7baa3c561c7e", "s63")]
        [TestCase("27067a02-df4b-49a1-8699-442b265a75d2", "s63")]
        [TestCase("0d91fb1a-cbe2-4443-8f61-e9a925fa00c9", "s63")]
        public async Task WhenICheckDownloadedZipForTypesInConfigFile_ThenZipIsCreatedForRequestedProduct(string batchId, string exchangeSetStandard)
        {
            string connectionString = "";
            string queueName = "";
            QueueClient queue = new QueueClient(connectionString, queueName);
            string queueMessage = File.ReadAllText("./TestData/BSQueueMessage.txt");
            queue.SendMessage(queueMessage);

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
