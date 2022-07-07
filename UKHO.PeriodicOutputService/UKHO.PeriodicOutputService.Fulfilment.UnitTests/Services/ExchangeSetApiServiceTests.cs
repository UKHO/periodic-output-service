using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetApiServiceTests
    {
        private IOptions<ExchangeSetApiConfiguration> _fakeExchangeSetApiConfiguration;
        private IExchangeSetApiClient _fakeExchangeSetApiClient;
        private IExchangeSetApiService _fakeExchangeSetApiService;
        private IAuthTokenProvider _fakeAuthTokenProvider;
        private ILogger<ExchangeSetApiService> _fakeLogger;


        [SetUp]
        public void Setup()
        {
            _fakeExchangeSetApiConfiguration = Options.Create(new ExchangeSetApiConfiguration() { EssClientId = "ClientId2" });

            _fakeExchangeSetApiClient = A.Fake<IExchangeSetApiClient>();

            _fakeAuthTokenProvider = A.Fake<IAuthTokenProvider>();

            _fakeLogger = A.Fake<ILogger<ExchangeSetApiService>>();

            _fakeExchangeSetApiService = new ExchangeSetApiService(_fakeLogger, _fakeExchangeSetApiConfiguration, _fakeExchangeSetApiClient, _fakeAuthTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
#pragma warning disable CS8625 // Cannot convert null literal to non-nullable reference type.
            Assert.Throws<ArgumentNullException>(
                () => new ExchangeSetApiService(null, _fakeExchangeSetApiConfiguration, _fakeExchangeSetApiClient, _fakeAuthTokenProvider))
                .ParamName
                .Should().Be("logger");

            Assert.Throws<ArgumentNullException>(
                () => new ExchangeSetApiService(_fakeLogger, null, _fakeExchangeSetApiClient, _fakeAuthTokenProvider))
                .ParamName
                .Should().Be("exchangeSetApiConfiguration");

            Assert.Throws<ArgumentNullException>(
               () => new ExchangeSetApiService(_fakeLogger, _fakeExchangeSetApiConfiguration, null, _fakeAuthTokenProvider))
               .ParamName
               .Should().Be("exchangeSetApiClient");

            Assert.Throws<ArgumentNullException>(
               () => new ExchangeSetApiService(_fakeLogger, _fakeExchangeSetApiConfiguration, _fakeExchangeSetApiClient, null))
               .ParamName
               .Should().Be("authTokenProvider");
#pragma warning restore CS8625 // Cannot convert null literal to non-nullable reference type.
        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenValidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeExchangeSetApiClient.GetProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                  });

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored));

            ExchangeSetGetBatchResponse response = await _fakeExchangeSetApiService.GetProductIdentifiersData(GetProductIdentifiers());
            Assert.Multiple(() =>
            {
                Assert.That(response.ExchangeSetCellCount, Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            });
        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenInvalidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeExchangeSetApiClient.GetProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.OK,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetInValidExchangeSetGetBatchResponse()))
                  });

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored));

            ExchangeSetGetBatchResponse response = await _fakeExchangeSetApiService.GetProductIdentifiersData(GetProductIdentifiers());
            Assert.Multiple(() =>
            {
                Assert.That(response.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response.RequestedProductsNotInExchangeSet, !Is.Null);
            });
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

        private ExchangeSetGetBatchResponse GetInValidExchangeSetGetBatchResponse() => new ExchangeSetGetBatchResponse
        {
            ExchangeSetCellCount = 0,
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
            },
            RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet()
        };

        private IEnumerable<RequestedProductsNotInExchangeSet> GetRequestedProductsNotInExchangeSet()
        {
            RequestedProductsNotInExchangeSet[] requestedProductsNotInExchangeSet = new RequestedProductsNotInExchangeSet[]
            {
                new RequestedProductsNotInExchangeSet {  ProductName="1US2ARCGD", Reason ="invalidProduct"}
            };

            return requestedProductsNotInExchangeSet;
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
