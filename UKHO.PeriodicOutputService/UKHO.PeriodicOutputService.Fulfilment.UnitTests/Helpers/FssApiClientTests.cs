using System.Net;
using FakeItEasy;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    [TestFixture]
    public class FssApiClientTests
    {
        private IFssApiClient? _fakeFssApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void DoesGetBatchStatusAsync_Returns_OK()
        {
            string authToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVh";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               authToken, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://test.com");

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fakeFssApiClient = new FssApiClient(_fakeHttpClientFactory);

            var result = _fakeFssApiClient.GetBatchStatusAsync("http://test.com", authToken);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Result.Content.ReadAsStringAsync().Result, Is.EqualTo(authToken));

            });
        }
    }
}
