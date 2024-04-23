using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Storage.Queues;
using FluentAssertions;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BespokeExchangeSetService
{
    [TestFixture]
    public class RequestAndDownloadExchangeSet
    {
        static readonly TestConfiguration testConfiguration = new();
        //private AzureTablesHelper azureTablesHelper { get; set; }

        [Test]
        [TestCase("s63", "UPDATE", "AVCS", "4bc70797-7ee6-407f-bafe-cae49a5b5f91")]
        [TestCase("s57", "CHANGE", "BLANK", "06583fac-dbce-4ea6-b67b-870392dcb7ab")]
        public async Task DownloadCustomExchangeSet(string exchangeSetStandard, string type, string readMeSearchFilter, string batchId)
        {
            //var queueMessage = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText("./TestData/BSQueueMessage.txt"));
            //queueMessage!.ReadMeSearchFilter = readMeSearchFilter;
            //queueMessage.ExchangeSetStandard = exchangeSetStandard;
            //string jsonString = JsonConvert.SerializeObject(queueMessage);

            var queueMessage = JsonConvert.DeserializeObject<BessConfig>(File.ReadAllText("./TestData/ConfigFile.json"));
            queueMessage!.Type = type;
            queueMessage.ExchangeSetStandard = exchangeSetStandard;
            queueMessage!.ReadMeSearchFilter = readMeSearchFilter;
            string jsonString = JsonConvert.SerializeObject(queueMessage);
            await BessUploadFileHelper.UploadConfigFileTest(testConfiguration.bessConfig.BaseUrl, jsonString, testConfiguration.sharedKeyConfig.Key);
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

    }
}
