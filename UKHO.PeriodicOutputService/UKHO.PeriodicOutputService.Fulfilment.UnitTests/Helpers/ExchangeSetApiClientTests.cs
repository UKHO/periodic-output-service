using System.Net;
using System.Text;
using FakeItEasy;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Providers;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using IHttpClientFactory = UKHO.PeriodicOutputService.Common.Factories.IHttpClientFactory;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    [TestFixture]
    public class ExchangeSetApiClientTests
    {
        private IExchangeSetApiClient _exchangeSetApiClient;
        private IHttpClientFactory _fakeHttpClientFactory;
        private IHttpClientFacade _fakeHttpClientFacade;

        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _fakeHttpClientFacade = A.Fake<IHttpClientFacade>();

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(false)).Returns(_fakeHttpClientFacade);

            _exchangeSetApiClient = new ExchangeSetApiClient(_fakeHttpClientFactory);
        }

        [Test]
        public void DoesProductIdentifiersData_Returns_OK()
        {
            var serializedProductIdentifierData = JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse());

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()), Encoding.UTF8, "application/json");

            A.CallTo(() => _fakeHttpClientFacade.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored)).Returns(response);

            var result = _exchangeSetApiClient.GetProductIdentifiersDataAsync("http://test.com", GetProductIdentifiers(), "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<ExchangeSetGetBatchResponse>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult.ExchangeSetCellCount, Is.EqualTo(GetValidExchangeSetGetBatchResponse().ExchangeSetCellCount));

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

