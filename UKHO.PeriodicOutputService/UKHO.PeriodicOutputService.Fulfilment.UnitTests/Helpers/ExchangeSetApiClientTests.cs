using System.Net;
using FakeItEasy;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    [TestFixture]
    public class ExchangeSetApiClientTests
    {
        private IExchangeSetApiClient? _exchangeSetApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
        }

        [Test]
        public void DoesProductIdentifiersData_Returns_OK()
        {
            var serializedProductIdentifierData = JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse());

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                                JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://test.com");

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _exchangeSetApiClient = new ExchangeSetApiClient(_fakeHttpClientFactory);

            var result = _exchangeSetApiClient.GetProductIdentifiersDataAsync("http://test.com", GetProductIdentifiers(), "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<ExchangeSetGetBatchResponse>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Has.Count.EqualTo(GetValidExchangeSetGetBatchResponse().ExchangeSetCellCount));

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

        private ExchangeSetGetBatchResponse GetValidExchangeSetGetBatchResponse() => new ExchangeSetGetBatchResponse
        {
            ExchangeSetCellCount = GetProductIdentifiers().Count,
            RequestedProductCount = GetProductIdentifiers().Count,
            Links = new Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                {
                    Href = "http://test1.com"
                },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href = "http://test2.com"
                },
                ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href = "http://test3.com"
                }
            }
        };
    }

}

