using System.Net;
using Microsoft.Extensions.Options;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.Common.Helpers;
using static UKHO.BESS.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.BespokeExchangeSetService
{
    [TestFixture]
    public class DownloadBespokeExchangeSet
    {
        static readonly TestConfiguration testConfiguration = new();
        readonly FssEndPointHelper fssEndPointHelper = new();
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
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
        }

        //PBI 140039 : BESS BS - Dealing with ancillary files : Get ReadMe.txt from FSS based on config
        //PBI 147178: BESS BS - Dealing with ancillary files : Delete PRODUCT.TXT file, INFO folder and update SERIAL.ENC
        //PBI 140040: BESS BS - Get permit from PKS and create key file (XML/TXT)
        [Test]
        [TestCase("s57", "UPDATE", "AVCS", "fa741049-7a78-4ec3-8737-1b3fb8d1cc3f", "KEY_TEXT")]
        [TestCase("s63", "BASE", "BLANK", "a7fb95f0-b3ff-4ef2-9b76-a74c7d3c3c8f", "PERMIT_XML")]
        [TestCase("s63", "CHANGE", "businessUnit eq 'AVCS-BESSets' and filename eq 'README.TXT' and $batch(Content) eq 'Bespoke README'", "5581ca8c-27a8-42ec-86d2-bef6915c2992", "PERMIT_XML")]
        [TestCase("s57", "UPDATE", "NONE", "9F19B71E-2DCA-42FD-B7DD-A68792A2AA27", "KEY_TEXT")]
        public async Task WhenIUploadAConfigWithCorrectDetails_ThenBespokeExchangeSetShouldBeDownloaded(string exchangeSetStandard, string type, string readMeSearchFilter, string batchId, string keyFileType)
        {
            HttpResponseMessage response = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath, testConfiguration.sharedKeyConfig.Key, exchangeSetStandard, type, readMeSearchFilter, keyFileType);
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)201));
            Extensions.WaitForDownloadExchangeSet();
            var downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId, true, keyFileType, true);
            var expectedResultReadme = FssBatchHelper.CheckReadMeInBessExchangeSet(downloadFolderPath, readMeSearchFilter);
            Assert.That(expectedResultReadme);
            var expectedResultSerial = FssBatchHelper.CheckInfoFolderAndSerialEncInBessExchangeSet(downloadFolderPath, type);
            Assert.That(expectedResultSerial);
            FssBatchHelper.VerifyCatalogContents(downloadFolderPath, readMeSearchFilter);
            HttpResponseMessage expectedResult = await fssEndPointHelper.CheckBatchDetails(batchId);
            await FssBatchHelper.VerifyBessBatchDetails(expectedResult);
            string? batchFolderPath = Path.GetDirectoryName(downloadFolderPath);
            bool expectedResulted = FssBatchHelper.VerifyPermitFile(batchFolderPath, keyFileType);
            Assert.That(expectedResulted);
        }

        //PBI 147171: BESS BS - Handling of empty ES and Error.txt Scenarios
        [Test]
        [TestCase("d0635e6c-81ae-4acb-9129-1a69f9ee58d2", "s57", "UPDATE", "NONE")]
        [TestCase("d0635e6c-81ae-4acb-9129-1a69f9ee58d2", "s57", "UPDATE", "KEY_TEXT")]
        public async Task WhenIProcessSameConfigWithCorrectDetailsTwice_ThenEmptyExchangeSetShouldBeDownloaded(string batchId, string exchangeSetStandard, string type, string keyFileType)
        {
            Extensions.AddQueueMessage(type, exchangeSetStandard, testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName, keyFileType);
            Extensions.WaitForDownloadExchangeSet();
            Extensions.AddQueueMessage(type, exchangeSetStandard, testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.QueueName, keyFileType);
            Extensions.WaitForDownloadExchangeSet();
            string downloadFolderPath = await EssEndpointHelper.CreateExchangeSetFile(batchId, false, null, true);
            FssBatchHelper.CheckFilesInEmptyBess(downloadFolderPath);
            string? batchFolderPath = Path.GetDirectoryName(downloadFolderPath);
            bool expectedResulted = FssBatchHelper.VerifyPermitFile(batchFolderPath, keyFileType);
            Assert.That(!expectedResulted);
        }

        [TearDown]
        public async Task TearDown()
        {
            //cleaning bessproductversiondetails azure table entries
            await Extensions.DeleteTableEntries(testConfiguration.AzureWebJobsStorage, testConfiguration.bessStorageConfig.TableName, testConfiguration.bessConfig.ProductsName);

            // Cleaning up config files from container.
            azureBlobStorageClient?.DeleteConfigsInContainer();

            //cleaning up the downloaded files from temp folder
            Extensions.DeleteTempDirectory(testConfiguration.bessConfig.TempFolderName);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = Extensions.Cleanup(testConfiguration.bessConfig.BaseUrl);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
        }
    }
}
