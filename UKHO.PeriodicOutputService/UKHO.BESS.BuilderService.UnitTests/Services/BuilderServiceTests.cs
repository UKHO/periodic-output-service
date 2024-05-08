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
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
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
        private ConfigQueueMessage configQueueMessage;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;
        private IOptions<FssApiConfiguration> fakeFssApiConfiguration;
        private const string readMeSearchFilterQuery = "$batch(Product Type) eq 'AVCS' and businessUnit eq 'ADDS'";
        private IPksService fakePksService;

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
                ReadMeFileName = "README.TXT"
            });

            fakeEssService = A.Fake<IEssService>();
            fakeFssService = A.Fake<IFssService>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<BuilderService.Services.BuilderService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakePksService = A.Fake<IPksService>();
            fakePermitDecryption = A.Fake<IPermitDecryption>();
            fakeConfiguration["IsFTRunning"] = "false";

            builderService = new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullEssService = () => new BuilderService.Services.BuilderService(null, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
            nullEssService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("essService");

            Action nullFssService = () => new BuilderService.Services.BuilderService(fakeEssService, null, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
            nullFssService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssService");

            Action nullFileSystemHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, null, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystemHelper");

            Action nullLogger = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, null, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullAzureTableStorageHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, null, fakeFssApiConfiguration, fakePksService, fakePermitDecryption);
            nullAzureTableStorageHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullFssApiConfiguration = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, null, fakePksService, fakePermitDecryption);
            nullFssApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssApiConfig");

            Action nullPksService = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, null, fakePermitDecryption);
            nullPksService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("pksService");

            Action nullPermitDecryption = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeFssApiConfiguration, fakePksService, null);
            nullPermitDecryption.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("permitDecryption");
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
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CreateXmlFromObject(A<PKSXml>.Ignored, A<string>.Ignored, A<string>.Ignored))
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
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(GetProductVersions);
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync((A<List<ProductVersion>>.Ignored), A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.CreateXmlFromObject(A<PKSXml>.Ignored, A<string>.Ignored, A<string>.Ignored))
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
            && call.GetArgument<EventId>(1) == EventIds.CreateBatchCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess batch created {DateTime} | {CorrelationId}"
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
        public async Task WhenTypeIsUpdateChangeAndKeyFileTypeIsText_ThenPostProductIdentifierEndpointIsCalledAndBespokeExchangeSetIsCreated(BessType type, ExchangeSetStandard exchangeSetStandard)
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
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
                .Returns(GetProductKeyServiceResponse());

            A.CallTo(() => fakePermitDecryption.GetPermitKeys(A<string>.Ignored)).Returns(new PermitKey() { ActiveKey = "12345", NextKey = "12345" });

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.KEY_TEXT));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
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
            && call.GetArgument<EventId>(1) == EventIds.CreateBatchCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess batch created {DateTime} | {CorrelationId}"
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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());


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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            await act.Should().ThrowAsync<Exception>().Where(x => x.Message.Contains("Extracting zip file"));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
            await act.Should().ThrowAsync<Exception>().Where(x => x.Message.Contains("Extracting zip file"));

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
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
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndBatchStatusIsNotCommitted_ThenCreateBespokeExchangeSetAsyncThrowsError(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
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
        public async Task WhenTypeIsUpdateOrChangeAndBatchStatusIsNotCommitted_ThenCreateBespokeExchangeSetAsyncThrowsError(BessType type, ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
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
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
              .Returns(GetProductKeyServiceResponse());
            A.CallTo(() => fakeAzureTableStorageHelper.SaveBessProductVersionDetailsAsync(A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML)); };
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
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

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
            && call.GetArgument<EventId>(1) == EventIds.CreateBatchCompleted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess batch created {DateTime} | {CorrelationId}"
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, readMeSearchFilterQuery, KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, readMeSearchFilterQuery, KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFssService.DownloadReadMeFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString(), KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard, ReadMeSearchFilter.BLANK.ToString(), KeyFileType.PERMIT_XML));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.CreateEmptyFileContent(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
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
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndKeyFileTypeIsNone_ThenPostProductIdentifierEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(new List<ProductVersion>());
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
              .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString()));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, true))
                .MustHaveHappenedOnceExactly();

            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
                .MustNotHaveHappened();

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
        [TestCase(ExchangeSetStandard.S63)]
        [TestCase(ExchangeSetStandard.S57)]
        public async Task WhenTypeIsBaseAndKeyFileTypeIsKey_Text_ThenPostProductIdentifiersEndpointIsCalledAndBespokeExchangeSetIsCreated(ExchangeSetStandard exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            A.CallTo(() => fakeFileSystemHelper.GetProductVersionsFromDirectory(A<string>.Ignored, A<string>.Ignored)).Returns(new List<ProductVersion>());
            A.CallTo(() => fakePksService.PostProductKeyData(A<List<ProductKeyServiceRequest>>.Ignored))
             .Returns(GetProductKeyServiceResponse());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE, exchangeSetStandard, ReadMeSearchFilter.AVCS.ToString(), KeyFileType.KEY_TEXT));

            result.Should().Be("Exchange Set Created Successfully");

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
                    RowKey= "s63|GB301910",
                    EditionNumber =10,
                    UpdateNumber =2
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
