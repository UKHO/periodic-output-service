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
        private IEssApiClient? _exchangeSetApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;

        [SetUp]
        public void Setup() => _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();

        [Test]
        public void DoesProductIdentifiersData_Returns_OK()
        {
            string? serializedProductIdentifierData = JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse());

            HttpMessageHandler? messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                                JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler)
            {
                BaseAddress = new Uri("http://test.com")
            };

            _ = A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _exchangeSetApiClient = new EssApiClient(_fakeHttpClientFactory);

            Task<HttpResponseMessage>? result = _exchangeSetApiClient.PostProductIdentifiersDataAsync("http://test.com", GetProductIdentifiers(), "asdfsa");

            ExchangeSetResponseModel? deSerializedResult = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Is.Not.Null);
            });
        }

        private List<string> GetProductIdentifiers() => new()
        {
                "US2ARCGD",
                "CA379151",
                "DE110000"
            };

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
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

