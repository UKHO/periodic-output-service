using System.IO;
using System.Net;
using Azure.Storage.Queues;
using FluentAssertions;
using Grpc.Core;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.PermitKeyService
{
    [TestFixture]
    public class PKSTests
    {
        static readonly TestConfiguration testConfiguration = new();
        static BessStorageConfiguration bessStorageConfiguration = testConfiguration.bessStorageConfig;
        AzureBlobStorageClient? azureBlobStorageClient;
        readonly dynamic config = Options.Create(new PeriodicOutputService.Common.Configuration.BessStorageConfiguration
        {
            ConnectionString = bessStorageConfiguration.ConnectionString!,
            ContainerName = bessStorageConfiguration.ContainerName!
        });

        [OneTimeSetUp]
        public void Setup()
        {   
            azureBlobStorageClient = new AzureBlobStorageClient(config);
            HttpResponseMessage apiResponse = Extensions.ConfigureFt(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.Identifiers);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        [TestCase("s63", "BASE", "PERMIT_XML", "BLANK", "a7fb95f0-b3ff-4ef2-9b76-a74c7d3c3c8f")]
        [TestCase("s57", "UPDATE", "KEY_TEXT", "AVCS", "fa741049-7a78-4ec3-8737-1b3fb8d1cc3f")]
        public async Task WhenICheckPermitFileInBES_ThenPermitFileIsCreatedAsPerConfig(string exchangeSetStandard, string type, string keyFileType, string readMeSearchFilter, string batchId)
        {
            HttpResponseMessage response = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath, testConfiguration.sharedKeyConfig.Key, exchangeSetStandard, type, readMeSearchFilter, keyFileType);
            response.StatusCode.Should().Be((HttpStatusCode)201);
            Extensions.WaitForDownloadExchangeSet();
            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            string batchFolderPath = downloadFolderPath.Remove(downloadFolderPath.Length-7);

            bool expectedResulted = FssBatchHelper.CheckPermitFile(batchFolderPath, keyFileType);
            expectedResulted.Should().Be(true);
            await Extensions.DeleteTableEntries(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.TableName, testConfiguration.bessConfig.ProductsName, exchangeSetStandard);
        }

        [TearDown]
        public void TearDown()
        {
            // Cleaning up config files from container.
            azureBlobStorageClient?.DeleteConfigsInContainer();

            //cleaning up the downloaded files from temp folder
            Extensions.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }
    }
}
