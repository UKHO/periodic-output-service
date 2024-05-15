using System.Net;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;


namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class SalesCatalogueClientTests
    {
        private ISalesCatalogueClient? salesCatalogueClient;
        private IHttpClientFactory fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void WhenSaleCatalogueSeviceApiIsCalled_ThenReturnsOk()
        {
            var httpResponseMessage = GetHttpResponseMessage(HttpStatusCode.OK);
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(JsonConvert.SerializeObject(httpResponseMessage), HttpStatusCode.OK);
            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://abc.com");

            A.CallTo(() => fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            salesCatalogueClient = new SalesCatalogueClient(fakeHttpClientFactory);

            var result = salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, "", "", httpClient.BaseAddress.ToString());
            var deSerializedResult = JsonConvert.DeserializeObject<HttpResponseMessage>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult.Should().NotBeNull();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest)]
        [TestCase(HttpStatusCode.Unauthorized)]
        [TestCase(HttpStatusCode.Forbidden)]
        public void WhenSaleCatalogueSeviceApiIsCalled_ThenReturnsNonOkResponse(HttpStatusCode statusCode)
        {
            var httpResponseMessage = GetHttpResponseMessage(statusCode);
            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(JsonConvert.SerializeObject(httpResponseMessage), statusCode);
            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://abc.com");

            A.CallTo(() => fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);
            salesCatalogueClient = new SalesCatalogueClient(fakeHttpClientFactory);

            var result = salesCatalogueClient.CallSalesCatalogueServiceApi(HttpMethod.Get, "", "", httpClient.BaseAddress.ToString());
            var deSerializedResult = JsonConvert.DeserializeObject<HttpResponseMessage>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(statusCode);
            deSerializedResult.Should().NotBeNull();
        }

        private HttpResponseMessage GetHttpResponseMessage(HttpStatusCode statusCode) => new()
        {
            StatusCode = statusCode,
            RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") },
            Content = new StringContent("", Encoding.UTF8, "application/json")
        };
    }
}
