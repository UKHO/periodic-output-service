﻿using System.Net;
using FakeItEasy;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class FleetManagerClientTests
    {
        private IFleetManagerApiClient? _fleetManagerApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void DoesGetJwtAuthUnpToken_Returns_OK()
        {
            var AuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVh";

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               AuthToken, HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://test.com");

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fleetManagerApiClient = new FleetManagerApiClient(_fakeHttpClientFactory);

            var result = _fleetManagerApiClient.GetJwtAuthUnpToken(HttpMethod.Get, "http://test.com", "credentials", "asdfsa");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Result.Content.ReadAsStringAsync().Result, Is.EqualTo(AuthToken));

            });
        }

        [Test]
        public void DoesGetCatalogue_Returns_OK()
        {
            var serializedProductIdentifier = JsonConvert.SerializeObject(GetProductIdentifiers());

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                               JsonConvert.SerializeObject(GetProductIdentifiers()), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://test.com");

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _fleetManagerApiClient = new FleetManagerApiClient(_fakeHttpClientFactory);

            var result = _fleetManagerApiClient.GetCatalogue(HttpMethod.Get, "http://test.com", "credentials", "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<List<string>>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Has.Count.EqualTo(GetProductIdentifiers().Count));

            });
        }

        private List<string> GetProductIdentifiers()
        {
            return new List<string>
            {
                "US2ARCGD",
                "CA379151",
                "DE110000"
            };
        }
    }

}
