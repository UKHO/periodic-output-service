using System.Net;
using FakeItEasy;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.UnitTests.Handler;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class EssApiClientTests
    {
        private EssApiClient? _essApiClient;
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

            _essApiClient = new EssApiClient(_fakeHttpClientFactory);

            var result = _essApiClient.PostProductIdentifiersDataAsync("http://test.com", GetProductIdentifiers(), "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Is.Not.Null);
            });
        }

        [Test]
        public void DoesGetProductDataSinceDateTime_Returns_OK()
        {
            var serializedProductDataSinceDateTimeData = JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse());

            var messageHandler = FakeHttpMessageHandler.GetHttpMessageHandler(
                                JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()), HttpStatusCode.OK);

            var httpClient = new HttpClient(messageHandler);
            httpClient.BaseAddress = new Uri("http://test.com");

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(A<string>.Ignored)).Returns(httpClient);

            _essApiClient = new EssApiClient(_fakeHttpClientFactory);

            var result = _essApiClient.GetProductDataSinceDateTime("http://test.com", DateTime.UtcNow.AddDays(-7).ToString("R"), "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<ExchangeSetResponseModel>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult, Is.Not.Null);
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

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new ExchangeSetResponseModel
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

