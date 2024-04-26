using System.Net;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Helpers;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BespokeExchangeSetService
{
    [TestFixture]
    public class RequestAndDownloadExchangeSet
    {
        static readonly TestConfiguration testConfiguration = new();
        static BessStorageConfiguration bessStorageConfiguration = testConfiguration.bessStorageConfig;
        AzureBlobStorageClient azureBlobStorageClient;
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

        //PBI 140039 : BESS BS - Dealing with ancillary files : Get ReadMe.txt from FSS based on config
        //PBI 147178: BESS BS - Dealing with ancillary files : Delete PRODUCT.TXT file, INFO folder and update SERIAL.ENC
        [Test]
        [TestCase("s57", "UPDATE", "AVCS", "fa741049-7a78-4ec3-8737-1b3fb8d1cc3f")]
        [TestCase("s63", "BASE", "BLANK", "a7fb95f0-b3ff-4ef2-9b76-a74c7d3c3c8f")]
        [TestCase("s63", "CHANGE", "businessUnit eq 'AVCS-BESSets' and filename eq 'README.TXT' and $batch(Content) eq 'Bespoke README'", "5581ca8c-27a8-42ec-86d2-bef6915c2992")]
        public async Task DownloadCustomExchangeSet(string exchangeSetStandard, string type, string readMeSearchFilter, string batchId)
        {
            HttpResponseMessage response = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath, testConfiguration.sharedKeyConfig.Key, exchangeSetStandard, type, readMeSearchFilter);
            response.StatusCode.Should().Be((HttpStatusCode)201);
            Extensions.WaitForDownloadExchangeSet();
            var downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId);
            var expectedResultReadme = FssBatchHelper.CheckReadMeInBessExchangeSet(downloadFolderPath, readMeSearchFilter);
            expectedResultReadme.Should().Be(true);
            var expectedResultSerial = FssBatchHelper.CheckInfoFolderAndSerialEncInBessExchangeSet(downloadFolderPath, type);
            expectedResultSerial.Should().Be(true);
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
