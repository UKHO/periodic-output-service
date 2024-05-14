using System.Net;
using FakeItEasy;
using FluentAssertions;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class PksApiClientTests
    {
        private IPksApiClient pksApiClient;
        private IHttpClientFactory fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void WhenValidProductKeyServiceDataIsPassed_ThenReturnsOKResponse()
        {
            string serializedProductIdentifierData = JsonConvert.SerializeObject(GetProductKeyServiceRequest());

            HttpMessageHandler messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(GetProductKeyServiceResponse()), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            pksApiClient = new PksApiClient(fakeHttpClientFactory);

            Task<HttpResponseMessage> result = pksApiClient.PostPksDataAsync("http://test.com", serializedProductIdentifierData, "asdfsa");

            List<ProductKeyServiceResponse> deSerializedResult = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            result.Result.StatusCode.Should().Be(HttpStatusCode.OK);
            deSerializedResult.Count.Should().BeGreaterThanOrEqualTo(1);
        }

        [Test]
        public void WhenInvalidProductKeyServiceDataIsPassed_ThenReturnsUnauthorizedResponse()
        {
            HttpMessageHandler messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                JsonConvert.SerializeObject(new List<ProductKeyServiceResponse>(){new()}), HttpStatusCode.Unauthorized);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            A.CallTo(() => fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            pksApiClient = new PksApiClient(fakeHttpClientFactory);

            Task<HttpResponseMessage> result = pksApiClient.PostPksDataAsync("http://test.com", "", null);

            result.Result.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        private static List<ProductKeyServiceRequest> GetProductKeyServiceRequest() =>
            new()
            {
                new(){ProductName = "DE260001", Edition = "1"}
            };

        private static List<ProductKeyServiceResponse> GetProductKeyServiceResponse()
        {
            return new List<ProductKeyServiceResponse>
            {
                new() { ProductName = "D0123456", Edition = "1", Key = "test" }
            };
        }
    }
}