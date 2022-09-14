using System.IO.Abstractions;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FssServiceTests
    {
        private IOptions<FssApiConfiguration> _fakeFssApiConfiguration;
        private ILogger<FssService> _fakeLogger;
        private IFssApiClient _fakeFssApiClient;
        private IAuthFssTokenProvider _fakeAuthFssTokenProvider;
        private IFssService _fssService;
        private IFileSystemHelper _fileSystemHelper;
        private IFileSystem _fakeFileSystem;

        [SetUp]
        public void Setup()
        {
            _fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                BaseUrl = "http://test.com",
                FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                BatchStatusPollingCutoffTime = "1",
                BatchStatusPollingDelayTime = "20000",
                PosReadUsers = "",
                PosReadGroups = "public"
            });

            _fakeLogger = A.Fake<ILogger<FssService>>();
            _fakeFssApiClient = A.Fake<IFssApiClient>();
            _fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();
            _fileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeFileSystem = A.Fake<IFileSystem>();

            _fssService = new FssService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider, _fileSystemHelper);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FssService(null, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider, _fileSystemHelper))
                .ParamName
                .Should().Be("logger");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, null, _fakeFssApiClient, _fakeAuthFssTokenProvider, _fileSystemHelper))
                .ParamName
                .Should().Be("fssApiConfiguration");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, _fakeFssApiConfiguration, null, _fakeAuthFssTokenProvider, _fileSystemHelper))
                .ParamName
                .Should().Be("fssApiClient");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, null, _fileSystemHelper))
                .ParamName
                .Should().Be("authFssTokenProvider");

            Assert.Throws<ArgumentNullException>(
                 () => new FssService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider, null))
                 .ParamName
                 .Should().Be("fileSystemHelper");
        }

        [Test]
        public async Task DoesCheckIfBatchCommitted_Returns_BatchStatus_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchStatusAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\":\"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\":\"Committed\"}")))
                });

            FssBatchStatus result = await _fssService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status");

            Assert.That(result, Is.AnyOf(FssBatchStatus.Incomplete, FssBatchStatus.Committed));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch status for BatchID - {BatchID} completed | Batch Status is {BatchStatus} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesCheckIfBatchCommitted_Throws_Exception_If_InvalidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchStatusAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"statusCode\":\"401\",\"message\":\"Authorization token is missing or invalid\"}")))
                });

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status"));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch status for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesCheckIfBatchCommitted_Returns_Error_If_TimedOut()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchStatusAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\":\"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\":\"CommitInProgress\"}")))
                });

            _fakeFssApiConfiguration.Value.BatchStatusPollingCutoffTime = "1";
            _fakeFssApiConfiguration.Value.BatchStatusPollingDelayTime = "500";

            Assert.ThrowsAsync<FulfilmentException>(
                () => _fssService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status"));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Fss batch status polling timed out for BatchID - {BatchID} failed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

        }

        [Test]
        public async Task DoesGetBatchDetails_Returns_BatchDetail_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchDetailsAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\": \"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\": \"Committed\",\"allFilesZipSize\": 11323697,\"attributes\": [{\"key\": \"Product Type\",\"value\": \"AVCS\"}],\"businessUnit\": \"AVCSCustomExchangeSets\",\"batchPublishedDate\": \"2022-07-13T10:53:58.98Z\",\"expiryDate\": \"2022-08-12T10:53:06Z\",\"files\": [{\"filename\": \"M01X02.zip\",\"fileSize\": 5095731,\"mimeType\": \"application/zip\",\"hash\": \"TLwn4f5J36mvWvrTafkXYA==\",\"attributes\": [],\"links\": {\"get\": {\"href\": \"/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip\"}}},{\"filename\": \"M02X02.zip\",\"fileSize\": 6267757,\"mimeType\": \"application/zip\",\"hash\": \"7tP0BwgbMdKZT8koKakR+w==\",\"attributes\": [],\"links\": {\"get\": {\"href\": \"/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M02X02.zip\"}}}]}")))
                });

            Common.Models.Fss.Response.GetBatchResponseModel? result = await _fssService.GetBatchDetails("4c5397d5-8a05-43fa-9009-9c38b2007f81");

            Assert.Multiple(() =>
            {
                Assert.That(result.BatchId, Is.EqualTo("4c5397d5-8a05-43fa-9009-9c38b2007f81"));
                Assert.That(result.Status, Is.EqualTo(FssBatchStatus.Committed.ToString()));
            });

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch details for BatchID - {BatchID} from FSS completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesGetBatchDetails_Throws_Exception_If_InvalidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchDetailsAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.GetBatchDetails("4c5397d5-8a05-43fa-9009-9c38b2007f81"));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch details for BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesDownloadFile_Returns_DownloadPath_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.DownloadFile(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            Stream? result = await _fssService.DownloadFile("M01X02", "/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip");

            Assert.That(result, Is.Not.Null);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Downloading of file {fileName} completed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesDownloadFile_Throws_Exception_If_InValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.DownloadFile(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\": \"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\": \"Committed\",\"allFilesZipSize\": 11323697,\"attributes\": [{\"key\": \"Product Type\",\"value\": \"AVCS\"}],\"businessUnit\": \"AVCSCustomExchangeSets\",\"batchPublishedDate\": \"2022-07-13T10:53:58.98Z\",\"expiryDate\": \"2022-08-12T10:53:06Z\",\"files\": [{\"filename\": \"M01X02.zip\",\"fileSize\": 5095731,\"mimeType\": \"application/zip\",\"hash\": \"TLwn4f5J36mvWvrTafkXYA==\",\"attributes\": [],\"links\": {\"get\": {\"href\": \"/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip\"}}},{\"filename\": \"M02X02.zip\",\"fileSize\": 6267757,\"mimeType\": \"application/zip\",\"hash\": \"7tP0BwgbMdKZT8koKakR+w==\",\"attributes\": [],\"links\": {\"get\": {\"href\": \"/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M02X02.zip\"}}}]}")))
                });

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.DownloadFile("M01X02", "/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip"));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Downloading of file {fileName} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesUploadBlocks_Returns_BlockIds_If_ValidRequest()
        {
            IFileInfo fileInfo = _fakeFileSystem.FileInfo.FromFileName("M01X01.zip");
            A.CallTo(() => fileInfo.Name).Returns("M01X01.zip");
            A.CallTo(() => fileInfo.Length).Returns(100);

            A.CallTo(() => _fakeFssApiClient.UploadFileBlockAsync(A<string>.Ignored, A<byte[]>.Ignored, A<byte[]>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(new HttpResponseMessage()
              {
                  StatusCode = System.Net.HttpStatusCode.OK,
                  RequestMessage = new HttpRequestMessage()
                  {
                      RequestUri = new Uri("http://test.com")
                  },
              });

            Task<List<string>>? result = _fssService.UploadBlocks("", fileInfo);

            Assert.That(result.Result.Count, Is.GreaterThan(0));
            Assert.That(result.Result.FirstOrDefault(), Does.Contain("Block"));

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesUploadBlocks_Throws_Exception_If_InValidRequest()
        {
            IFileInfo fileInfo = _fakeFileSystem.FileInfo.FromFileName("M01X01.zip");
            A.CallTo(() => fileInfo.Name).Returns("M01X01.zip");
            A.CallTo(() => fileInfo.Length).Returns(100);

            A.CallTo(() => _fakeFssApiClient.UploadFileBlockAsync(A<string>.Ignored, A<byte[]>.Ignored, A<byte[]>.Ignored, A<string>.Ignored, A<string>.Ignored))
              .Returns(new HttpResponseMessage()
              {
                  StatusCode = System.Net.HttpStatusCode.Unauthorized,
                  RequestMessage = new HttpRequestMessage()
                  {
                      RequestUri = new Uri("http://test.com")
                  },
              });

            Assert.ThrowsAsync<AggregateException>(() => _fssService.UploadBlocks("", fileInfo));

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
              .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to upload block {BlockID} of {FileName} failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesCreateBatch_Throws_Exception_If_InValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.CreateBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.CreateBatch(Batch.PosFullAvcsIsoSha1Batch));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to create batch for {BatchType} in FSS failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesCreateBatch_Returns_BatchId_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.CreateBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,

                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\":\"4c5397d5-8a05-43fa-9009-9c38b2007f81\"}")))
                });

            var result = await _fssService.CreateBatch(Batch.PosFullAvcsIsoSha1Batch);

            Assert.That(result, Is.EqualTo("4c5397d5-8a05-43fa-9009-9c38b2007f81"));

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "New batch for {BatchType} created in FSS. Batch ID is {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesAddFileToBatch_Returns_True_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.AddFileToBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Created,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            bool result = await _fssService.AddFileToBatch("4c5397d5-8a05-43fa-9009-9c38b2007f81", "filename.txt", 2453443233);

            Assert.That(result, Is.True);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File {FileName} is added in batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesAddFileToBatch_Returns_False_If_InValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.AddFileToBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<long>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            Assert.ThrowsAsync<FulfilmentException>(
            () => _fssService.AddFileToBatch("4c5397d5-8a05-43fa-9009-9c38b2007f81", "filename.txt", 2453443233));

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to add file {FileName} to batch with BatchID - {BatchID} failed | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesWriteBlockFile_Returns_True_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.WriteBlockInFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Created,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            IEnumerable<string> blockIds = new List<string> { "Block_00001", "Block_00001" };

            bool result = await _fssService.WriteBlockFile("4c5397d5-8a05-43fa-9009-9c38b2007f81", "filename.txt", blockIds);

            Assert.That(result, Is.True);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "File blocks written in file {FileName} for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesWriteBlockFile_Throws_Exception_If_InValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.WriteBlockInFileAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            IEnumerable<string> blockIds = new List<string> { "Block_00001", "Block_00001" };

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.WriteBlockFile("4c5397d5-8a05-43fa-9009-9c38b2007f81", "filename.txt", blockIds));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to write blocks in file {FileName} failed for batch with BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesCommitBatch_Returns_True_If_ValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.CommitBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Created,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            IEnumerable<string> fileNames = new List<string> { "MX0101.zip", "MX0201.zip" };

            bool result = await _fssService.CommitBatch("4c5397d5-8a05-43fa-9009-9c38b2007f81", fileNames);

            Assert.That(result, Is.True);

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Information
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch with BatchID - {BatchID} committed in FSS | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
             ).MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void DoesCommitBatch_Returns_False_If_InValidRequest()
        {
            A.CallTo(() => _fakeFssApiClient.CommitBatchAsync(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                });

            IEnumerable<string> fileNames = new List<string> { "MX0101.zip", "MX0201.zip" };

            Assert.ThrowsAsync<FulfilmentException>(() => _fssService.CommitBatch("4c5397d5-8a05-43fa-9009-9c38b2007f81", fileNames));

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch commit failed for BatchID - {BatchID} | {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
             .MustHaveHappenedOnceExactly();
        }
    }
}
