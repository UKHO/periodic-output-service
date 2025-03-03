using System.Net;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Helpers;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.MockEndPoint
{
    public class UploadBessConfigFileTests
    {
        private static readonly TestConfiguration testConfiguration = new();
        private static readonly BessStorageConfiguration bessStorageConfiguration = testConfiguration.bessStorageConfig;
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
        }

        //PBI 142682: BESS CS : Create a mock endpoint to upload BESS configuration files to azure storage
        [Test]
        public async Task WhenICallUploadConfigApiWithValidConfigFile_ThenCreatedStatusCode201IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath, testConfiguration.sharedKeyConfig.Key, "s63", "UPDATE", "AVCS", "PERMIT_XML");
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)201));
        }

        //PBI 142682: BESS CS : Create a mock endpoint to upload BESS configuration files to azure storage
        [Test]
        public async Task WhenICallUploadConfigApiWithInvalidConfigFile_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.InvalidConfigPath, testConfiguration.sharedKeyConfig.Key, "s63", "UPDATE", "AVCS", "PERMIT_XML");
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)400));
        }

        [TearDown]
        public void TearDown()
        {
            // Cleaning up config files from container.
            azureBlobStorageClient?.DeleteConfigsInContainer();
        }
    }
}
