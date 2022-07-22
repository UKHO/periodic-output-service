using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
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

        [SetUp]
        public void Setup()
        {
            _fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                BaseUrl = "http://test.com",
                FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                BatchStatusPollingCutoffTime = "1",
                BatchStatusPollingDelayTime = "20000"
            });

            _fakeLogger = A.Fake<ILogger<FssService>>();
            _fakeFssApiClient = A.Fake<IFssApiClient>();
            _fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();

            _fssService = new FssService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FssService(null, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("logger");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, null, _fakeFssApiClient, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("fssApiConfiguration");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, _fakeFssApiConfiguration, null, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("fssApiClient");

            Assert.Throws<ArgumentNullException>(
                () => new FssService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, null))
                .ParamName
                .Should().Be("authFssTokenProvider");
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
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\":\"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\":\"Incomplete\"}")))
                });

            FssBatchStatus result = await _fssService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status");

            Assert.That(result, Is.AnyOf(FssBatchStatus.Incomplete, FssBatchStatus.Committed));

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

        }

        [Test]
        public async Task DoesCheckIfBatchCommitted_Returns_BatchStatus_If_InvalidRequest()
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

            FssBatchStatus result = await _fssService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status");

            Assert.That(result, Is.EqualTo(FssBatchStatus.Incomplete));

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch status for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}"
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

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesGetBatchDetails_Returns_LogError_If_InvalidRequest()
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

            Common.Models.Fss.Response.GetBatchResponseModel? result = await _fssService.GetBatchDetails("4c5397d5-8a05-43fa-9009-9c38b2007f81");

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to get batch details for BatchID - {BatchId} failed at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();
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

            Stream? result = await _fssService.DownloadFile("D:\\downloadPath", "M01X02", "/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip");

            Assert.That(result, Is.Not.Null);

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task DoesDownloadFile_Returns_LogError_If_InValidRequest()
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

            Stream? result = await _fssService.DownloadFile("D:\\downloadPath", "M01X02", "/batch/621e8d6f-9950-4ba6-bfb4-92415369aaee/files/M01X02.zip");

            Assert.That(result, Is.Null);

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();
        }
    }
}
