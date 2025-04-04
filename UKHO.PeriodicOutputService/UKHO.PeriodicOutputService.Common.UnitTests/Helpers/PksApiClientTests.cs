﻿using System.Net;
using FakeItEasy;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class PksApiClientTests
    {
        private IPksApiClient? pksApiClient;
        private IHttpClientFactory fakeHttpClientFactory;
        private string fakeCorrelationId = Guid.NewGuid().ToString();

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

            Task<HttpResponseMessage> result = pksApiClient.PostPksDataAsync("http://test.com", serializedProductIdentifierData, "asdfsa", fakeCorrelationId);

            List<ProductKeyServiceResponse> deSerializedResult = JsonConvert.DeserializeObject<List<ProductKeyServiceResponse>>(result.Result.Content.ReadAsStringAsync().Result);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Has.Count.EqualTo(1));
            }
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

            Task<HttpResponseMessage> result = pksApiClient.PostPksDataAsync("http://test.com", "", null, fakeCorrelationId);

            Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.Unauthorized));
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
