using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BespokeExchangeSetService
{
    [Ignore("Temp")]
    [TestFixture]
    public class RequestAndDownloadExchangeSet
    {
        static readonly TestConfiguration testConfiguration = new();
        static BessStorageConfiguration bessStorageConfiguration = testConfiguration.bessStorageConfig;
        AzureBlobStorageClient azureBlobStorageClient;
        readonly dynamic test = Options.Create(new PeriodicOutputService.Common.Configuration.BessStorageConfiguration { ConnectionString = bessStorageConfiguration.ConnectionString,
        ContainerName = bessStorageConfiguration.ContainerName });
    
        [OneTimeSetUp]
        public void Setup()
        {
             azureBlobStorageClient = new AzureBlobStorageClient(test);
            HttpResponseMessage apiResponse = Extensions.ConfigureFt(testConfiguration.bessConfig.BaseUrl, "Identifiers");
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        [TestCase("s57", "UPDATE", "AVCS", "fa741049-7a78-4ec3-8737-1b3fb8d1cc3f")]
        [TestCase("s63", "BASE", "BLANK", "a7fb95f0-b3ff-4ef2-9b76-a74c7d3c3c8f")]
        public async Task DownloadCustomExchangeSet(string exchangeSetStandard, string type, string readMeSearchFilter, string batchId)
        {
            //var queueMessage = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText("./TestData/BSQueueMessage.txt"));
            //queueMessage!.ReadMeSearchFilter = readMeSearchFilter;
            //queueMessage.ExchangeSetStandard = exchangeSetStandard;
            //string jsonString = JsonConvert.SerializeObject(queueMessage);

            var configDetails = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText("./TestData/ConfigFile.json"));
            configDetails!.Name = "BES-123" + Extensions.RandomNumber();
            configDetails!.Type = type;
            configDetails.ExchangeSetStandard = exchangeSetStandard;
            configDetails!.ReadMeSearchFilter = readMeSearchFilter;
            string jsonString = JsonConvert.SerializeObject(configDetails);
            HttpResponseMessage response = await BessUploadFileHelper.UploadConfigFileTest(testConfiguration.bessConfig.BaseUrl, jsonString, testConfiguration.sharedKeyConfig.Key);
            //fileName = await response.Content.ReadAsStringAsync();
            //QueueClientOptions queueOptions = new() { MessageEncoding = QueueMessageEncoding.Base64 };
            //QueueClient queue = new QueueClient(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName, queueOptions);
            //queue.SendMessage(jsonString);

            ////bool expectedResult = FssBatchHelper.CheckInfoFolderAndSerialENCInCustomExchangeSet("D://HOME//BESS//f8fd2fb4-3dd6-425d-b34f-3059e262feed//V01X01", type);
            ////expectedResult.Should().Be(true);
            Extensions.WaitForDownloadExchangeSet();
            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            var expectedResult = FssBatchHelper.CheckReadMeInCustomExchangeSet(downloadFolderPath, readMeSearchFilter);
            expectedResult.Should().Be(true);
        }


        [TearDown]
        public void TearDown()
        {
            // Cleaning up config files from container.
            azureBlobStorageClient.DeleteConfigsInContainer();

            //cleaning up the downloaded files from temp folder
            Extensions.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }
    }
}
