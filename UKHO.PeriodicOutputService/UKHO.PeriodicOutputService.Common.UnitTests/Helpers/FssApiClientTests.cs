using System.Net;
using System.Text;
using FakeItEasy;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class FssApiClientTests
    {
        private IHttpClientFactory _fakeHttpClientFactory;
        private FssApiClient? _fssApiClient;
        private const string AuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVh";

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void DoesGetBatchStatusAsync_Returns_OK()
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(AuthToken, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.GetBatchStatusAsync("http://test.com", AuthToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Result.Content.ReadAsStringAsync().Result, Is.EqualTo(AuthToken));
            });
        }

        [Test]
        public void DoesCreateBatchAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.CreateBatchAsync("http://test.com", content, AuthToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesAddFileInBatchAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.AddFileToBatchAsync("http://test.com", content, AuthToken, 1231231, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesUploadFileBlockAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.UploadFileBlockAsync("http://test.com", Encoding.UTF8.GetBytes("whatever"), Encoding.UTF8.GetBytes("whatever"), AuthToken, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesWriteBlockInFileAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.WriteBlockInFileAsync("http://test.com", content, AuthToken, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesCommitBatchAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.CommitBatchAsync("http://test.com", content, AuthToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesDownloadFile_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.DownloadFile("http://test.com", AuthToken, "bytes=1-1024");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }

        [Test]
        public void DoesDownloadFile_With_Client_That_Allow_Redirect_As_False_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.DownloadFile("http://test.com", AuthToken);

            Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void DoesGetBatchDetailsAsync_Returns_OK()
        {
            const string content = "";
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(content, HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler) { BaseAddress = new Uri("http://test.com") };
            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            _fssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fssApiClient.GetBatchDetailsAsync("http://test.com", AuthToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
            });
        }
    }
}
