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
        private FssApiClient? _fakeFssApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;
        private readonly string _authToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVh";

        [SetUp]
        public void Setup() => _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();

        [Test]
        public void DoesGetBatchStatusAsync_Returns_OK()
        {
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               _authToken, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.GetBatchStatusAsync("http://test.com", _authToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Result.Content.ReadAsStringAsync().Result, Is.EqualTo(_authToken));

            });
        }

        [Test]
        public void DoesCreateBatchAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.CreateBatchAsync("http://test.com", content, _authToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesAddFileInBatchAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.AddFileToBatchAsync("http://test.com", content, _authToken, 1231231, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesUploadFileBlockAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.UploadFileBlockAsync("http://test.com", Encoding.UTF8.GetBytes("whatever"), Encoding.UTF8.GetBytes("whatever"), _authToken, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesWriteBlockInFileAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.WriteBlockInFileAsync("http://test.com", content, _authToken, "application/octet-stream");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesCommitBatchAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.CommitBatchAsync("http://test.com", content, _authToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesDownloadFile_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.DownloadFile("http://test.com", _authToken, "bytes=1-1024");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }

        [Test]
        public void DoesDownloadFile_With_Client_That_Allow_Redirect_As_False_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.DownloadFile("http://test.com", _authToken);

            Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        }

        [Test]
        public void DoesGetBatchDetailsAsync_Returns_OK()
        {
            var content = "";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               content, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.GetBatchDetailsAsync("http://test.com", _authToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            });
        }
    }
}
