using System.IO.Abstractions;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
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
        private IFileInfo fakeFileInfo;
        private ConfigQueueMessage? configQueueMessage;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;
        private IOptions<FssApiConfiguration> fakeFssApiConfiguration;
        private const string readMeSearchFilterQuery = "$batch(Product Type) eq 'AVCS' and businessUnit eq 'ADDS'";

        [SetUp]
        public void Setup()
        {
            fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                BaseUrl = "http://test.com",
                FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                BatchStatusPollingCutoffTime = "1",
                BatchStatusPollingDelayTime = "20000",
                BatchStatusPollingCutoffTimeForAIO = "1",
                BatchStatusPollingDelayTimeForAIO = "20000",
                BatchStatusPollingCutoffTimeForBES = "1",
                BatchStatusPollingDelayTimeForBES = "20000",
                PosReadUsers = "",
                PosReadGroups = "public",
                BlockSizeInMultipleOfKBs = 4096,
                BespokeExchangeSetFileFolder = "V01X01",
                EncRoot = "ENC_ROOT",
                ReadMeFileName = "README.TXT",
                SerialFileName = "SERIAL.ENC",
                ProductFileName = "PRODUCT.TXT",
                Info = "INFO"
            });

            fakeEssService = A.Fake<IEssService>();
            fakeFssService = A.Fake<IFssService>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<BuilderService.Services.BuilderService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFileInfo = A.Fake<IFileInfo>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakeConfiguration["IsFTRunning"] = "false";

            builderService = new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullEssService = () => new BuilderService.Services.BuilderService(null, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration);
            nullEssService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("essService");

            Action nullFssService = () => new BuilderService.Services.BuilderService(fakeEssService, null, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration);
            nullFssService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssService");

            Action nullFileSystemHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, null, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystemHelper");

            Action nullLogger = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, null, fakeAzureTableStorageHelper, fakeFssApiConfiguration);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullAzureTableStorageHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, null, fakeFssApiConfiguration);
            nullAzureTableStorageHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullFssApiConfiguration = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, null);
            nullFssApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssApiConfig");
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBase_ThenPostProductIdentifiersEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
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
            A.CallTo(() => fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored))
                .MustHaveHappenedOnceOrMore();

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
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch {besBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
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
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
                .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
                .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
                .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
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
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch {besBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

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
        public async Task WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

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
        public async Task WhenTypeIsBaseAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

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
        public async Task WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

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
        public async Task WhenTypeIsBaseAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<Exception>().Where(x => x.Message.Contains("Extracting zip file"));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
        public async Task WhenTypeIsUpdateOrChangeAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<Exception>().Where(x => x.Message.Contains("Extracting zip file"));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
        public async Task WhenTypeIsBaseAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.FssPollingCutOffTimeout.ToEventId());

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
        public async Task WhenTypeIsUpdateOrChangeAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.FssPollingCutOffTimeout.ToEventId());

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
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenLoggingProductVersionDetailsInAzureFails_ThenCreateBespokeExchangeSetsThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
             .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync(A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<Exception>().Where(x => x.Message.Contains("Logging Product version failed"));

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.ProductsFetchedFromESS.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Error
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsFailed.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndBespokeExchangeSetIsCreated_ThenSerialENCContentIsUpdatedAndProductFileIsDeleted(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .Returns("GBWK06-24   20240208UPDATE    02.00U01X01");
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            result.Should().Be(true);

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
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
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
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch {besBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsStarted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<EventId>(1) == EventIds.LoggingProductVersionsCompleted.ToEventId()
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public void WhenTypeIsUpdateOrChangeAndUpdateSerialFileAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.UPDATE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())))
                .Should().ThrowAsync<Exception>().WithMessage("SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}");


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
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.CHANGE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())))
                .Should().ThrowAsync<Exception>().WithMessage("PRODUCT.TXT file and INFO folder delete operation failed at { DateTime} | { ErrorMessage} | _X - Correlation - ID:{ CorrelationId}");

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
        public async Task WhenTypeIsBaseAndValidReadmeSearchFilter_BespokeExchangeSetIsCreated_ThenStandardReadmeIsReplaced(ExchangeSetStandard exchangeSetStandard)
        {
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.SearchReadMeFilePathAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(filePath);
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, readMeSearchFilterQuery));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .MustHaveHappenedOnceExactly();

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
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndValidReadmeSearchFilter_BespokeExchangeSetIsCreated_ThenStandardReadmeIsReplaced(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            string filePath = @"D:\\Downloads";
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.SearchReadMeFilePathAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .Returns(filePath);
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, readMeSearchFilterQuery));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

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
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service search query request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.DownloadReadMeFileRequestStart.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File share service download request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndReadmeSearchFilterIsBLANK_ThenBespokeExchangeSetIsCreatedAndBlankReadmeReplaced(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString()));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

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
        public async Task WhenTypeIsUpdateOrChangeAndReadmeSearchFilterIsBLANK_ThenBespokeExchangeSetIsCreatedAndBlankReadmeReplaced(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored))
               .Returns(Guid.NewGuid().ToString());
            A.CallTo(() => fakeFileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });
            A.CallTo(() => fakeFileSystemHelper.GetFileInfo(A<string>.Ignored))
                .Returns(fakeFileInfo);
            A.CallTo(() => fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored))
                .Returns(true);
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString()));

            result.Should().Be(true);

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();

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
        [TestCase(ExchangeSetStandard.S57)]
        [TestCase(ExchangeSetStandard.S63)]
        public void WhenTypeIsBaseAndCreationOfZipFileFails_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())))
                .Should().ThrowAsync<Exception>().WithMessage("Creating zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())))
            .Should().ThrowAsync<Exception>().WithMessage("Creating zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
        public async Task WhenTypeIsBaseAndCreationOfBatchFails_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId.Equals(EventIds.BESBatchCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S63)]
        [TestCase(BessType.UPDATE, ExchangeSetStandard.S57)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S63)]
        [TestCase(BessType.CHANGE, ExchangeSetStandard.S57)]
        public async Task WhenTypeIsUpdateOrChangeAndCreationOfBatchFails_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => fakeFssService.CreateBatch(A<Batch>.Ignored, A<ConfigQueueMessage>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString())); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId.Equals(EventIds.BESBatchCreationFailed.ToEventId()));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.BESBatchCreationFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BES batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        #region PrivateMethods

        private ConfigQueueMessage GetConfigQueueMessage(BessType type, ExchangeSetStandard exchangeSetStandard, string readMeSearchFilter)
        {
            configQueueMessage = new ConfigQueueMessage()
            {
                Name = "test",
                ExchangeSetStandard = exchangeSetStandard.ToString(),
                EncCellNames = type == BessType.BASE ? new List<string> { "testcell" } : new string[] { "testcellforversion" },
                Frequency = "30 13 * * *",
                Type = type.ToString(),
                KeyFileType = "NONE",
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

        #endregion PrivateMethods
    }
}
