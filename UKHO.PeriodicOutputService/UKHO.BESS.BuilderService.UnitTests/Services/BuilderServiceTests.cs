﻿using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.BESS.BuilderService.Services;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
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
        private ConfigQueueMessage configQueueMessage;

        [SetUp]
        public void Setup()
        {
            fakeEssService = A.Fake<IEssService>();
            fakeFssService = A.Fake<IFssService>();
            fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            fakeLogger = A.Fake<ILogger<BuilderService.Services.BuilderService>>();
            fakeConfiguration = A.Fake<IConfiguration>();

            builderService = new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullEssSerivce = () => new BuilderService.Services.BuilderService(null, fakeFssService, fakeConfiguration, fakeFileSystemHelper, fakeLogger);
            nullEssSerivce.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("essService");

            Action nullFssSerivce = () => new BuilderService.Services.BuilderService(fakeEssService, null, fakeConfiguration, fakeFileSystemHelper, fakeLogger);
            nullFssSerivce.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fssService");

            Action nullFileSystemHelper = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, null, fakeLogger);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystemHelper");

            Action nullLogger = () => new BuilderService.Services.BuilderService(fakeEssService, fakeFssService, fakeConfiguration, fakeFileSystemHelper, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public async Task WhenTypeIsBase_ThenPostProductIdentifierEndpointIsCalledAndBespokeExchangeSetIsCreated(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard));

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
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public async Task WhenTypeIsUpdateOrChange_ThenGetProductVersionEndpointIsCalledAndBespokeExchangeSetIsCreated(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            var result = await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard));

            result.Should().Be("Exchange Set Created Successfully");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
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
        [TestCase("s63")]
        [TestCase("s57")]
        public void WhenTypeIsBaseAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsFileNameError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public void WhenTypeIsBaseAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndGetBatchFilesContainsNoBatchFiles_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithNoBatchFiles());

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.ErrorFileFoundInBatch.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ErrorFileFoundInBatch.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("s63")]
        [TestCase("s57")]
        public void WhenTypeIsBaseAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)))
                .Should().ThrowAsync<Exception>().WithMessage("Extracting zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndExtractExchangeSetZipThrowsError_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.Committed);
            A.CallTo(() => fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());
            A.CallTo(() => fakeFileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<Exception>().WithMessage("Extracting zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}");

            A.CallTo(() => fakeFileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();
            A.CallTo(() => fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.ExtractZipFileStarted.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
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
        public void WhenTypeIsBaseAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored))
                .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(BessType.BASE.ToString(), exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.FssPollingCutOffTimeout.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"

            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FssPollingCutOffTimeout.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase("UPDATE", "s63")]
        [TestCase("UPDATE", "s57")]
        [TestCase("CHANGE", "s63")]
        [TestCase("CHANGE", "s57")]
        public void WhenTypeIsUpdateOrChangeAndBatchStatusIsNotCommited_ThenCreateBespokeExchangeSetAsyncThrowsError(string type, string exchangeSetStandard)
        {
            A.CallTo(() => fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());
            A.CallTo(() => fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            FluentActions.Invoking(async () => await builderService.CreateBespokeExchangeSetAsync(GetConfigQueueMessage(type, exchangeSetStandard)))
                .Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.FssPollingCutOffTimeout.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FssPollingCutOffTimeout.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
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
               new BatchFile
               {
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
               new BatchFile
               {
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
        #endregion PrivateMethods
    }
}