using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class ExchangeSetApiServiceTests
    {
        private IOptions<ExchangeSetApiConfiguration> _fakeExchangeSetApiConfiguration;
        private IExchangeSetApiClient _fakeExchangeSetApiClient;
        private IExchangeSetApiService _exchangeSetApiService;
        private IAuthEssTokenProvider _fakeAuthTokenProvider;
        private ILogger<ExchangeSetApiService> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            _fakeExchangeSetApiConfiguration = Options.Create(new ExchangeSetApiConfiguration() { EssClientId = "ClientId2" });
            _fakeExchangeSetApiClient = A.Fake<IExchangeSetApiClient>();
            _fakeAuthTokenProvider = A.Fake<IAuthEssTokenProvider>();
            _fakeLogger = A.Fake<ILogger<ExchangeSetApiService>>();

            _exchangeSetApiService = new ExchangeSetApiService(_fakeLogger, _fakeExchangeSetApiConfiguration, _fakeExchangeSetApiClient, _fakeAuthTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
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
               .Should().Be("authEssTokenProvider");
        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenValidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeExchangeSetApiClient.PostProductIdentifiersDataAsync
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


            ExchangeSetResponseModel response = await _exchangeSetApiService.PostProductIdentifiersData(GetProductIdentifiers());
            Assert.Multiple(() =>
            {
                Assert.That(response.ExchangeSetCellCount, Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed getting exchange set details"
            ).MustNotHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

        }

        [Test]
        public async Task DoesGetProductIdentifiersData_Returns_ValidData_WhenInvalidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeExchangeSetApiClient.PostProductIdentifiersDataAsync
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


            ExchangeSetResponseModel response = await _exchangeSetApiService.PostProductIdentifiersData(GetProductIdentifiers());
            Assert.Multiple(() =>
            {
                Assert.That(response.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response.RequestedProductsNotInExchangeSet, !Is.Null);
            });

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed getting exchange set details"
            ).MustNotHaveHappened();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

        }

        [Test]
        public async Task DoesGetProductIdentifiersData_LogsError_When_ResponseStatus_Is_Not_OK()
        {
            A.CallTo(() => _fakeExchangeSetApiClient.PostProductIdentifiersDataAsync
            (A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored))
                  .Returns(new HttpResponseMessage()
                  {
                      StatusCode = HttpStatusCode.NotFound,
                      RequestMessage = new HttpRequestMessage()
                      {
                          RequestUri = new Uri("http://test.com")
                      },
                      Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                  });


            ExchangeSetResponseModel response = await _exchangeSetApiService.PostProductIdentifiersData(GetProductIdentifiers());

            A.CallTo(_fakeLogger).Where(call =>
             call.Method.Name == "Log"
             && call.GetArgument<LogLevel>(0) == LogLevel.Error
             && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post productidentifiers to ESS at {DateTime} | StatusCode:{StatusCode} | _X-Correlation-ID:{CorrelationId}"
             ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

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

        private ExchangeSetResponseModel GetInValidExchangeSetGetBatchResponse() => new ExchangeSetResponseModel
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
