using System.IO.Abstractions;
using Azure.Storage.Blobs;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.PermitDecryption;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.UnitTests.Services
{
    [TestFixture]
    public class BuilderServiceTests
    {
        private IBuilderService builderService;
        private IEssService fakeEssService;
        private IFssService fakeFssService;
        private IFileSystemHelper fakeFileSystemHelper;
        private ILogger<BuilderService.Services.BuilderService> fakeLogger;
        private IConfiguration fakeConfiguration;
        private IPermitDecryption fakePermitDecryption;
        private IFileInfo fakeFileInfo;
        private ConfigQueueMessage? configQueueMessage;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;
        private IOptions<FssApiConfiguration> fakeFssApiConfiguration;
        private const string readMeSearchFilterQuery = "$batch(Product Type) eq 'AVCS' and businessUnit eq 'ADDS'";
        private IPksService fakePksService;
        private ICatalog031Helper fakeCatalog031Helper;
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;

        [SetUp]
        public void Setup()
        {
            fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                BaseUrl = "http://test.com",
                FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                BatchStatusPollingCutoffTime = "0.1",
                BatchStatusPollingDelayTime = "100",
                BatchStatusPollingCutoffTimeForAIO = "0.1",
                BatchStatusPollingDelayTimeForAIO = "100",
                BatchStatusPollingCutoffTimeForBESS = "0.1",
                BatchStatusPollingDelayTimeForBESS = "100",
                PosReadUsers = "",
                PosReadGroups = "public",
                BlockSizeInMultipleOfKBs = 4096,
                EncRoot = "ENC_ROOT",
                ReadMeFileName = "README.TXT",
                SerialFileName = "SERIAL.ENC",
                ProductFileName = "PRODUCT.TXT",
                Info = "INFO",
                BessZipFileName = "BESS_test",
                BespokeExchangeSetFileFolder = "V01X01"
            });

            fakeEssService = A.Fake<IEssService>();
            fakeFssService = A.Fake<IFssService>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<BuilderService.Services.BuilderService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFileInfo = A.Fake<IFileInfo>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakePksService = A.Fake<IPksService>();
            fakePermitDecryption = A.Fake<IPermitDecryption>();
            fakeConfiguration["IsFTRunning"] = "false";
            fakePksService = A.Fake<IPksService>();
            fakeCatalog031Helper = A.Fake<ICatalog031Helper>();
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            builderService = new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);

            var fakeBlobClient = A.Fake<BlobClient>();
            var messageDetail = GetMessageDetail(BessType.BASE);
            var messageDetailJson = JsonConvert.SerializeObject(messageDetail);

            A.CallTo(() => fakeAzureBlobStorageClient.GetBlobClientByUriAsync(A<string>.Ignored))
                .Returns(fakeBlobClient);

            A.CallTo(() => fakeAzureBlobStorageClient.DownloadBlobContentAsync(A<BlobClient>.Ignored))
                .Returns(messageDetailJson);

            A.CallTo(() => fakeAzureBlobStorageClient.DeleteBlobContentAsync(A<BlobClient>.Ignored))
                .Returns(Task.CompletedTask);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullEssService = () => new BuilderService.Services.BuilderService(null, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            var exception = Assert.Throws<ArgumentNullException>(() => nullEssService());
            Assert.That(exception.ParamName, Is.EqualTo("essService"));

            Action nullFssService = () => new BuilderService.Services.BuilderService(fakeEssService, null, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullFssService());
            Assert.That(exception.ParamName, Is.EqualTo("fssService"));

            Action nullFileSystemHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, null, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullFileSystemHelper());
            Assert.That(exception.ParamName, Is.EqualTo("fileSystemHelper"));

            Action nullLogger = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, null, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullLogger());
            Assert.That(exception.ParamName, Is.EqualTo("logger"));

            Action nullAzureTableStorageHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, null, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullAzureTableStorageHelper());
            Assert.That(exception.ParamName, Is.EqualTo("azureTableStorageHelper"));

            Action nullFssApiConfiguration = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, null, fakePksService, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullFssApiConfiguration());
            Assert.That(exception.ParamName, Is.EqualTo("fssApiConfig"));

            Action nullPksService = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, null, fakePermitDecryption, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullPksService());
            Assert.That(exception.ParamName, Is.EqualTo("pksService"));

            Action nullPermitDecryption = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, null, fakeCatalog031Helper, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullPermitDecryption());
            Assert.That(exception.ParamName, Is.EqualTo("permitDecryption"));

            Action nullCatalog031Helper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, null, fakeAzureBlobStorageClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullCatalog031Helper());
            Assert.That(exception.ParamName, Is.EqualTo("catalog031Helper"));

            Action nullAzureBlobStorageClient = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption, fakeCatalog031Helper, null);
            exception = Assert.Throws<ArgumentNullException>(() => nullAzureBlobStorageClient());
            Assert.That(exception.ParamName, Is.EqualTo("azureBlobStorageClient"));
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBase_ThenPostProductIdentifiersEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {       
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .Returns(GetProductKeyServiceResponse());
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceOrMore();

            A.CallTo(() => fakeFileSystemHelper.CreateXmlFromObject(A<PksXml>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch: {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChange_ThenGetProductVersionsEndpointIsCalledAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
                .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
                .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync((A<List<ProductVersion>>.Ignored), A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CreateXmlFromObject(A<PksXml>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch: {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version details for Config Name:{configName} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateChangeAndKeyFileTypeIsText_ThenPostProductVersionsEndpointIsCalledAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .Returns(GetProductKeyServiceResponse());

            A.CallTo(() => fakePermitDecryption.GetPermitKeys(A<string>.Ignored, A<string>.Ignored)).Returns(new PermitKey() { ActiveKey = "12345", NextKey = "12345" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.KEY_TEXT));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync((A<List<ProductVersion>>.Ignored), A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CreateTextFile(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch: {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version details for Config Name:{configName} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public void WhenTypeIsBaseAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.ErrorFileFoundInBatch.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.ErrorFileFoundInBatch.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public void WhenTypeIsBaseAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.ErrorFileFoundInBatch.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.ErrorFileFoundInBatch.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public void WhenTypeIsBaseAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.ExtractZipFileFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.ExtractZipFileFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored)).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public void WhenTypeIsBaseAndBatchStatusIsNotCommitted_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.FssPollingCutOffTimeout.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FssPollingCutOffTimeout.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndBatchStatusIsNotCommitted_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.FssPollingCutOffTimeout.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FssPollingCutOffTimeout.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        //[TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        //[TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        //[TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenLoggingProductVersionDetailsInAzureFails_ThenCreateBespokeExchangeSetsThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .Returns(GetProductKeyServiceResponse());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync(A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.LoggingProductVersionsFailed.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version details for Config Name:{configName} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Error
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsFailed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version failed | {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndBespokeExchangeSetIsCreated_ThenSerialENCContentIsUpdatedAndProductFileIsDeleted(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .Returns("GBWK06-24   20240208UPDATE    02.00U01X01");
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
                .MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored))
                .MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessSerialEncUpdated.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SERIAL.ENC file updated with Type: {exchangeSetType} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessProductTxtAndInfoFolderDeleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "PRODUCT.TXT file and INFO folder deleted | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChange_ThenGetProductVersionsEndpointIsCalledAndProductIsNotAvailableOnAzureTableBespokeExchangeSetIsCreatedWithZeroEditionAndUpdate(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(new List<ProductVersionEntities>() { });
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync((A<List<ProductVersion>>.Ignored), A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch: {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version details for Config Name:{configName} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndUpdateSerialFileAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.UPDATE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.BessSerialEncUpdateFailed.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BessSerialEncUpdateFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndDeleteProductTxtAndInfoFolderAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.CHANGE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "PRODUCT.TXT file and INFO folder delete operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndValidReadmeSearchFilter_ThenStandardReadmeIsReplacedAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.SearchReadMeFilePathAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(filePath);
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, readMeSearchFilterQuery, KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme.txt file for _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme.txt file for _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndValidReadmeSearchFilter_ThenStandardReadmeIsReplacedAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.SearchReadMeFilePathAsync(A<string>.Ignored, A<string>.Ignored))
               .Returns(filePath);
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, readMeSearchFilterQuery, KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.QueryFileShareServiceReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme.txt file for _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme.txt file for _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndReadmeSearchFilterIsBLANK_ThenBlankReadmeReplacedAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndReadmeSearchFilterIsBLANK_ThenBlankReadmeReplacedAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndKeyFileTypeIsNone_ThenPostProductIdentifierEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(new List<ProductVersion>());
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndEmptyBatchResponseFromESS_ThenBespokeExchangeSetCreatedAndAzureTableNotUpdated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(new List<ProductVersion>());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString()));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync((A<List<ProductVersion>>.Ignored), A<string>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.EmptyBatchResponse.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Latest edition/update details not found. | DateTime: {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SkipPksAsEmptyExchangeSetFoundForProducts.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Key Service request was skipped because an Empty Exchange Set was found for the requested product(s) | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustNotHaveHappened();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S57)]
        [TestCase(ExchangeSetStandard.S63)]
        public void WhenTypeIsBaseAndCreationOfZipFileFails_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

           AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.ZipFileCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndCreationOfZipFileFails_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.ZipFileCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S57)]
        [TestCase(ExchangeSetStandard.S63)]
        public void WhenTypeIsBaseAndCreationOfBatchFails_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.BessBatchCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndCreationOfBatchFails_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored)).Throws<Exception>();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            var exception = Assert.ThrowsAsync<AggregateException>(act);
            Assert.That((exception.InnerException as FulfilmentException)?.EventId, Is.EqualTo(EventIds.BessBatchCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndKeyFileTypeIsKey_Text_ThenPostProductIdentifiersEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
             .Returns(GetProductKeyServiceResponse());
            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.KEY_TEXT));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBase_AndNoLatestProductVersionFoundThenEmptyBespokeExchangeSetIsCreatedWithNoPermitFile(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(new List<ProductVersion>() { });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
              .MustNotHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateXmlFromObject(A<PksXml>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ZipFileCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BessBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS batch: {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.SkipPksAsEmptyExchangeSetFoundForProducts.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product Key Service request was skipped because an Empty Exchange Set was found for the requested product(s) | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndReadmeSearchFilterIsNONE_ThenReadmeFileDeletedAndRemovedReadMeEntryFromCatalogFileAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .DoesNothing();

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.NONE.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .MustNotHaveHappened();
            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.BessReadMeFileDeleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "README.TXT file deleted. | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndReadmeSearchFilterIsNONE_ThenReadmeFileDeletedAndRemovedReadMeEntryFromCatalogFileAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored, A<string>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(new List<string> { "Block_00001" });
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .DoesNothing();

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.NONE.ToString(), KeyFileType.PERMIT_XML));

            Assert.That(result);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileInfo.MoveTo(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
              .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .MustNotHaveHappened();
            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ZipFileRenamed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Zip file {fileName} renamed to file {newFileName} at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.PermitFileCreationCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.BessReadMeFileDeleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "README.TXT file deleted. | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndDeleteReadmeTxtAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).Throws<Exception>();
            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .DoesNothing();

            AsyncTestDelegate act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.CHANGE, exchangeSetStandard, ReadMeSearchFilter.NONE.ToString())); };
            var exception = Assert.ThrowsAsync<FulfilmentException>(act);
            Assert.That(exception.EventId, Is.EqualTo(EventIds.BessReadMeFileDeletionFailed.ToEventId()));

            A.CallTo(() => fakeCatalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(A<string>.Ignored))
                .MustNotHaveHappened();
            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BessReadMeFileDeletionFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "README.TXT file delete operation failed. | ErrorMessage: {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored)).MustNotHaveHappened();
        }

        #region PrivateMethods

        private ConfigQueueMessage GetConfigQueueMessage(BessType type, ExchangeSetStandard exchangeSetStandard, string readMeSearchFilter, KeyFileType keyFileType = KeyFileType.NONE)
        {
            configQueueMessage = new ConfigQueueMessage()
            {
                Name = "test",
                ExchangeSetStandard = exchangeSetStandard.ToString(),
                EncCellNames = type == BessType.BASE ? new List<string> { "testcell" } : new string[] { "testcellforversion" },
                Frequency = "30 13 * * *",
                Type = type.ToString(),
                KeyFileType = keyFileType.ToString(),
                AllowedUsers = new string[] { "testuser" },
                AllowedUserGroups = new string[] { "testgroup" },
                Tags = new List<Tag> { new() { Key = "key1", Value = "value1" }, new() { Key = "key2", Value = "value2" } },
                ReadMeSearchFilter = readMeSearchFilter,
                BatchExpiryInDays = 8,
                IsEnabled = "Yes",
                FileName = "test.json",
                FileSize = 1,
                CorrelationId = "384b3783-df9c-4378-a342-47523dc1c7ef"
            };

            return configQueueMessage;
        }

        private MessageDetail GetMessageDetail(BessType type)
        {
            return new MessageDetail
            {
                EncCellNames = type == BessType.BASE
                    ? new List<string> { "testcell" }
                    : new [] { "testcellforversion" }
            };
        }

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = 3,
            RequestedProductCount = 3,
            Links = new PeriodicOutputService.Common.Models.Ess.Response.Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                {
                    Href = "http://test1.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href = "http://test2.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                },
                ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href = "http://test3.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                }
            },
            RequestedProductsNotInExchangeSet = new List<RequestedProductsNotInExchangeSet>(),
            ResponseDateTime = DateTime.UtcNow
        };

        private static GetBatchResponseModel GetValidBatchResponseModel() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files = new List<BatchFile>
            {
               new() {
                   Filename = "V01X01.zip",
                   Links = new PeriodicOutputService.Common.Models.Fss.Response.Links
                   {
                       Get = new Link
                       {
                           Href ="http://test1.com"
                       }
                   }
               }
            }
        };

        private static GetBatchResponseModel GetBatchResponseModelWithFileNameError() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files = new List<BatchFile>
            {
               new() {
                   Filename = "Error.txt",
                   Links = new PeriodicOutputService.Common.Models.Fss.Response.Links
                   {
                       Get = new Link
                       {
                           Href ="http://test1.com"
                       }
                   }
               }
            }
        };

        private static GetBatchResponseModel GetBatchResponseModelWithNoBatchFiles() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files = new List<BatchFile>()
        };

        private static List<ProductVersionEntities> GetProductVersionEntities() => new()
        {
            new ProductVersionEntities
            {
                PartitionKey = "Port of London",
                RowKey = "s63|GB301910",
                EditionNumber = 10,
                UpdateNumber = 2
            }
        };

        private static readonly IEnumerable<ProductVersion> GetProductVersions = new List<ProductVersion>()
        {
                new()
                {
                      EditionNumber =1,
                      ProductName = "testcellforversion",
                      UpdateNumber= 10
                }
        };

        private static List<ProductKeyServiceResponse> GetProductKeyServiceResponse() => new()
        {
                new ProductKeyServiceResponse
                {
                   ProductName = "D0123456",
                   Edition = "1",
                   Key = "test123"
                }
        };

        #endregion PrivateMethods
    }
}
