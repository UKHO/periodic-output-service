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
        private IOptions<BessStorageConfiguration> fakeBessStorageConfiguration;
        private ConfigQueueMessage? configQueueMessage;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;

        [SetUp]
        public void Setup()
        {
            fakeEssService = A.Fake<IEssService>();
            fakeFssService = A.Fake<IFssService>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<BuilderService.Services.BuilderService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakeBessStorageConfiguration = A.Fake<IOptions<BessStorageConfiguration>>();

            builderService = new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeBessStorageConfiguration);

            fakeBessStorageConfiguration.Value.ExchangeSetFolder = "V01X01";
            fakeBessStorageConfiguration.Value.SerialFileName = "SERIAL.ENC";
            fakeBessStorageConfiguration.Value.ProductFileName = "PRODUCT.TXT";
            fakeBessStorageConfiguration.Value.Info = "INFO";
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullEssSerivce = () => new BuilderService.Services.BuilderService(null, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeBessStorageConfiguration);
            nullEssSerivce.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("essService");

            Action nullFssSerivce = () => new BuilderService.Services.BuilderService(fakeEssService, null, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, fakeBessStorageConfiguration);
            nullFssSerivce.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssService");

            Action nullFileSystemHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, null, fakeLogger, fakeAzureTableStorageHelper, fakeBessStorageConfiguration);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystemHelper");

            Action nullLogger = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, null, fakeAzureTableStorageHelper, fakeBessStorageConfiguration);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullAzureTableStorageHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, null, fakeBessStorageConfiguration);
            nullAzureTableStorageHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullBessStorageConfiguration = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger, fakeAzureTableStorageHelper, null);
            nullBessStorageConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("bessStorageConfiguration");
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBase_ThenPostProductIdentifiersEndpointIsCalledAndBespokeExchangeSetIsCreated(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored))
             .Returns(true);
            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard));

            result.Should().Be("Exchange Set Created Successfully");

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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChange_ThenGetProductVersionsEndpointIsCalledAndBespokeExchangeSetIsCreated(string type, string exchangeSetStandard)
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard));

            result.Should().Be("Exchange Set Created Successfully");

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
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBaseAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)); };
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
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBaseAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)); };
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
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBaseAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)); };
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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChangeAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)); };
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
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBaseAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.FssPollingCutOffTimeout.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"

            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FssPollingCutOffTimeout.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChangeAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetLatestBessProductVersionDetailsAsync()).Returns(GetProductVersionEntities());
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)); };
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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenLoggingProductVersionDetailsInAzureFails_ThenCreateBespokeExchangeSetsThrowsError(string type, string exchangeSetStandard)
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

            Func<Task> act = async () => { await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)); };
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

        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBaseAndBespokeExchangeSetIsCreated_ThenSerialENCContentIsUpdatedAndProductFileIsDeleted(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored))
                .Returns("GBWK06-24   20240208UPDATE    02.00U01X01");

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard));

            result.Should().Be("Exchange Set Created Successfully");

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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChange_ThenGetProductVersionsEndpointIsCalledAndProductIsNotAvailableOnAzureTableBespokeExchangeSetIsCreatedWithZeroEditionAndUpdate(string type, string exchangeSetStandard)
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

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard));

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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndUpdateSerialFileAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<Exception>().WithMessage("SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}");


            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.SerialEncUpdateFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.CreateFileContent(A<string>.Ignored, A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndDeleteProductTxtAndInfoFolderAsyncThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<Exception>().WithMessage("PRODUCT.TXT file and INFO folder delete operation failed at { DateTime} | { ErrorMessage} | _X - Correlation - ID:{ CorrelationId}");

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ProductTxtDeleteFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "PRODUCT.TXT file and INFO folder delete operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.ReadFileText(A<string>.Ignored)).MustHaveHappened();
            A.CallTo(() => fakeFileSystemHelper.DeleteFolder(A<string>.Ignored)).MustNotHaveHappened();
        }

        #region PrivateMethods

        private ConfigQueueMessage GetConfigQueueMessage(string type, string exchangeSetStandard)
        {
            configQueueMessage = new ConfigQueueMessage()
            {
                Name = "test",
                ExchangeSetStandard = exchangeSetStandard,
                EncCellNames = type == BessType.BASE.ToString() ? new List<string> { "testcell" } : new string[] { "testcellforversion" },
                Frequency = "30 13 * * *",
                Type = type,
                KeyFileType = "NONE",
                AllowedUsers = new string[] { "testuser" },
                AllowedUserGroups = new string[] { "testgroup" },
                Tags = new List<Tag> { new() { Key = "key1", Value = "value1" }, new() { Key = "key2", Value = "value2" } },
                ReadMeSearchFilter = "NONE",
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

        #endregion PrivateMethods
    }
}
