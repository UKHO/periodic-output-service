﻿using System.IO.Abstractions;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.AdmiraltyInformationOverlay.Fulfilment.Services;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.AdmiraltyInformationOverlay.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTests
    {
        private FulfilmentDataService _fulfilmentDataService;
        private IEssService _fakeEssService;
        private IFssService _fakeFssService;
        private ILogger<FulfilmentDataService> _fakeLogger;
        private IFileSystemHelper _fakefileSystemHelper;
        private IConfiguration _fakeconfiguration;
        private IFileInfo _fakeFileInfo;
        private IAzureTableStorageHelper _fakeAzureTableStorageHelper;
        private IOptions<FssApiConfiguration> _fakeFssApiConfiguration;

        [SetUp]
        public void Setup()
        {
            _fakeEssService = A.Fake<IEssService>();
            _fakeFssService = A.Fake<IFssService>();
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakefileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeconfiguration = A.Fake<IConfiguration>();
            _fakeFileInfo = A.Fake<IFileInfo>();
            _fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();

            _fakeconfiguration["IsFTRunning"] = "false";
            _fakeconfiguration["AioCells"] = "GB800001";
            _fakeconfiguration["WeeksToIncrement"] = "1";

            _fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                SerialFileName = "SERIAL.AIO"
            });

            _fulfilmentDataService = new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, _fakeFssService, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new FulfilmentDataService(null, _fakeEssService, _fakeFssService, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("fileSystemHelper"));

            exception = Assert.Throws<ArgumentNullException>(
                () => new FulfilmentDataService(_fakefileSystemHelper, null, _fakeFssService, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("essService"));

            exception = Assert.Throws<ArgumentNullException>(
                () => new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, null, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("fssService"));

            exception = Assert.Throws<ArgumentNullException>(
                () => new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, _fakeFssService, null, _fakeconfiguration, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("logger"));

            exception = Assert.Throws<ArgumentNullException>(
                 () => new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, _fakeFssService, _fakeLogger, null, _fakeAzureTableStorageHelper, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("configuration"));

            exception = Assert.Throws<ArgumentNullException>(
                 () => new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, _fakeFssService, _fakeLogger, _fakeconfiguration, null, _fakeFssApiConfiguration));
            Assert.That(exception.ParamName, Is.EqualTo("azureTableStorageHelper"));

            exception = Assert.Throws<ArgumentNullException>(
                () => new FulfilmentDataService(_fakefileSystemHelper, _fakeEssService, _fakeFssService, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper, null));
            Assert.That(exception.ParamName, Is.EqualTo("fssApiConfig"));


        }

        [Test]
        public async Task Does_CreateAioExchangeSets_Executes_Successfully()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakefileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns([@"D:\Test"]);

            A.CallTo(() => _fakeFssService.CreateBatch(A<Batch>.Ignored, A<FormattedWeekNumber>.Ignored))
                           .Returns(Guid.NewGuid().ToString());

            A.CallTo(() => _fakeFileInfo.Name).Returns("AIO.zip");
            A.CallTo(() => _fakeFileInfo.Length).Returns(100990);

            A.CallTo(() => _fakefileSystemHelper.GetFileInfo(A<string>.Ignored))
                          .Returns(_fakeFileInfo);

            A.CallTo(() => _fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored, A<string>.Ignored))
                .Returns(["Block_00001"]);

            A.CallTo(() => _fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
              .Returns(true);

            var result = await _fulfilmentDataService.CreateAioExchangeSetsAsync();

            Assert.That(result, Is.True);

            A.CallTo(() => _fakefileSystemHelper.CreateDirectory(A<string>.Ignored))
              .MustHaveHappened(3, Times.Exactly);

            A.CallTo(() => _fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => _fakeFileInfo.MoveTo(A<string>.Ignored))
              .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
               .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => _fakefileSystemHelper.CreateIsoAndSha1(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappened(2, Times.Exactly);

            A.CallTo(() => _fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<string>.Ignored))
                .MustHaveHappenedOnceOrMore();

            #region Log checks

            A.CallTo(_fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Getting latest product version details started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Getting latest product version details completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of update exchange set for Productversions - {Productversions} started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Update exchange set created successfully | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of AIO base exchange set started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creation of AIO base exchange set completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch for AIO base CD created by ESS successfully with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
                ).MustHaveHappened(2, Times.Exactly);

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating ISO and Sha1 file of {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Information
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
              ).MustHaveHappened(2, Times.Exactly);

            #endregion Log checks
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_Batch_Is_Not_Committed()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
            .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.CommitInProgress);

            Assert.ThrowsAsync<FulfilmentException>(
                 () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_Extraction_Fails()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
            .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Assert.ThrowsAsync<ArgumentNullException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(() => _fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_CreateIsoAndSha1ForExchangeSet_Fails()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
            .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakefileSystemHelper.CreateIsoAndSha1(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).Throws<ArgumentNullException>();

            Assert.ThrowsAsync<ArgumentNullException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(() => _fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating ISO and Sha1 file of {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating ISO and Sha1 file of {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating ISO and Sha1 file of {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_Blank_AIO_Cell_Is_Passed()
        {
            _fakeconfiguration["AioCells"] = string.Empty;

            Assert.ThrowsAsync<FulfilmentException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "AIO cells are empty in configuration | {DateTime} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .MustNotHaveHappened();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_If_GetBatchFiles_Contains_FileName_Error()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            Assert.ThrowsAsync<FulfilmentException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_If_GetBatchFiles_Contains_FileName_V01X01()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameV01X01());

            Assert.ThrowsAsync<FulfilmentException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "The configuration of the AIO cell is not synchronized with the ESS. V01X01 file found in AIO batch - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_Logging_Product_Version_Details_In_Azure_Fails()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored, A<Batch>.Ignored, A<string>.Ignored))
             .Returns(true);

            A.CallTo(() => _fakeAzureTableStorageHelper.SaveProductVersionDetails(A<List<ProductVersion>>.Ignored)).Throws<Exception>();

            Assert.ThrowsAsync<Exception>(
               () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
              call.Method.Name == "Log"
              && call.GetArgument<LogLevel>(0) == LogLevel.Error
              && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
              ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_CreateExchangeSetZip_Fails()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored, A<RequestType>.Ignored, A<string>.Ignored))
              .Returns(FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored, A<string>.Ignored))
            .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakefileSystemHelper.CreateZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<ArgumentNullException>();

            Assert.ThrowsAsync<ArgumentNullException>(
                () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(() => _fakeFssService.DownloadFileAsync(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Information
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}"
               ).MustNotHaveHappened();

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_AioExchangeSetCellCount_Is_Zero()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetInValidExchangeSetGetBatchRespGetProductDataProductVersionsonseWithZeroAIOCells());

            Assert.ThrowsAsync<FulfilmentException>(
              () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Due to the empty exchange set, ESS validation failed while producing an update | {DateTime} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateAioExchangeSets_Throws_Error_When_Requested_Invalid_Products_In_ExchangeSet()
        {
            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
             .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeEssService.GetProductDataProductVersions(A<ProductVersionsRequest>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(GetInValidExchangeSetGetBatchResponseWithRequestedInvalidProductsNotInExchangeSet());

            Assert.ThrowsAsync<FulfilmentException>(
             () => _fulfilmentDataService.CreateAioExchangeSetsAsync());

            A.CallTo(_fakeLogger).Where(call =>
               call.Method.Name == "Log"
               && call.GetArgument<LogLevel>(0) == LogLevel.Error
               && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS validation failed for {Count} products [{Products}] while creating update exchange set {DateTime} | _X-Correlation-ID : {CorrelationId}"
               ).MustHaveHappenedOnceExactly();
        }

        private static ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
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
            AioExchangeSetCellCount = 1,
            RequestedProductsNotInExchangeSet = [],
            ResponseDateTime = DateTime.UtcNow
        };

        private static ExchangeSetResponseModel GetInValidExchangeSetGetBatchRespGetProductDataProductVersionsonseWithZeroAIOCells() => new()
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
            AioExchangeSetCellCount = 0,
            RequestedProductsNotInExchangeSet = [],
            ResponseDateTime = DateTime.UtcNow
        };

        private static ExchangeSetResponseModel GetInValidExchangeSetGetBatchResponseWithRequestedInvalidProductsNotInExchangeSet() => new()
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
            AioExchangeSetCellCount = 1,
            RequestedProductsNotInExchangeSet =
                                                    [
                                                        new RequestedProductsNotInExchangeSet
                                                            {
                                                            ProductName="ABC00001",
                                                            Reason= "Invalid"
                                                        }
                                                    ],
            ResponseDateTime = DateTime.UtcNow
        };

        private static GetBatchResponseModel GetValidBatchResponseModel() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files =
            [
               new BatchFile
               {
                   Filename = "AIO.zip",
                   Links = new PeriodicOutputService.Common.Models.Fss.Response.Links
                   {
                       Get = new Link
                       {
                           Href ="http://test1.com"
                       }
                   }
               }
            ]
        };

        private static GetBatchResponseModel GetBatchResponseModelWithFileNameError() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files =
            [
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
            ]
        };

        private static GetBatchResponseModel GetBatchResponseModelWithFileNameV01X01() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files =
            [
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
            ]
        };
    }
}
