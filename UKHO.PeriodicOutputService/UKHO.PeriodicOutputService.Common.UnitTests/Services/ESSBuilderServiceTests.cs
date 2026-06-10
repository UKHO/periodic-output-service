using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Services
{
    [TestFixture]
    public class ESSBuilderServiceTests
    {
        private IOptions<EssApiConfiguration> _fakeEssApiConfiguration;
        private IEssApiClient _fakeEssApiClient;
        private IAuthEssTokenProvider _fakeAuthTokenProvider;
        private ILogger<EssBuilderService> _fakeLogger;

        private IEssBuilderService _essBuilderService;

        [SetUp]
        public void Setup()
        {
            _fakeEssApiConfiguration = Options.Create(new EssApiConfiguration() { EssClientId = "ClientId2", BaseUrl = "http://base.url" });
            _fakeEssApiClient = A.Fake<IEssApiClient>();
            _fakeAuthTokenProvider = A.Fake<IAuthEssTokenProvider>();
            _fakeLogger = A.Fake<ILogger<EssBuilderService>>();

            _essBuilderService = new EssBuilderService(_fakeLogger, _fakeEssApiConfiguration, _fakeEssApiClient, _fakeAuthTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Parameter_Is_Null()
        {
            var exception = Assert.Throws<ArgumentNullException>((Action)(() => new EssBuilderService(null, _fakeEssApiConfiguration, _fakeEssApiClient, _fakeAuthTokenProvider)));
            Assert.That(exception.ParamName, Is.EqualTo("logger"));

            exception = Assert.Throws<ArgumentNullException>((Action)(() => new EssBuilderService(_fakeLogger, null, _fakeEssApiClient, _fakeAuthTokenProvider)));
            Assert.That(exception.ParamName, Is.EqualTo("essApiConfiguration"));

            exception = Assert.Throws<ArgumentNullException>((Action)(() => new EssBuilderService(_fakeLogger, _fakeEssApiConfiguration, null, _fakeAuthTokenProvider)));
            Assert.That(exception.ParamName, Is.EqualTo("essApiClient"));

            exception = Assert.Throws<ArgumentNullException>((Action)(() => new EssBuilderService(_fakeLogger, _fakeEssApiConfiguration, _fakeEssApiClient, null)));
            Assert.That(exception.ParamName, Is.EqualTo("authEssTokenProvider"));
        }

        [Test]
        public async Task PostProductIdentifiersData_Returns_ValidData_WhenValidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync(A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                });

            var response = await _essBuilderService.PostProductIdentifiersData(GetProductIdentifiers(), ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Standard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response?.ExchangeSetCellCount, Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            }

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task PostProductIdentifiersData_Returns_ValidData_WhenInvalidProductIdentifiersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync(A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetInvalidExchangeSetGetBatchResponse()))
                });

            var response = await _essBuilderService.PostProductIdentifiersData(GetProductIdentifiers(), ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Large.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response?.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response?.RequestedProductsNotInExchangeSet, !Is.Null);
            }

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void PostProductIdentifiersData_LogsError_When_ResponseStatus_Is_Not_OK()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync(A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                });

            Assert.ThrowsAsync<FulfilmentException>((Func<Task>)(async () => await _essBuilderService.PostProductIdentifiersData(GetProductIdentifiers(), ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Standard.ToString())));

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void PostProductIdentifiersData_LogsError_When_ResponseStatus_Is_NotModified()
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync(A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotModified,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                });

            Assert.ThrowsAsync<FulfilmentException>((Func<Task>)(async () => await _essBuilderService.PostProductIdentifiersData(GetProductIdentifiers(), ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Large.ToString())));

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task GetProductDataSinceDateTime_Returns_ValidData_WhenValidParametersArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                    Headers = { Date = DateTime.UtcNow }
                });

            var response = await _essBuilderService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-7).ToString("R"), ExchangeSetStandard.S57.ToString(), ExchangeSetLayout.Standard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            }

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task GetProductDataSinceDateTime_Returns_ValidData_WhenInvalidProductsReturned()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetInvalidExchangeSetGetBatchResponse())),
                    Headers = { Date = DateTime.UtcNow }
                });

            var response = await _essBuilderService.GetProductDataSinceDateTime(DateTime.UtcNow.AddDays(-1).ToString("R"), ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Large.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response?.ExchangeSetCellCount, !Is.EqualTo(GetProductIdentifiers().Count));
                Assert.That(response?.RequestedProductsNotInExchangeSet, !Is.Null);
            }

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void GetProductDataSinceDateTime_LogsError_When_ResponseStatus_Is_Not_OK()
        {
            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns("InvalidToken");
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                });

            Assert.ThrowsAsync<FulfilmentException>((Func<Task>)(async () => await _essBuilderService.GetProductDataSinceDateTime(DateTime.UtcNow.ToString("R"), ExchangeSetStandard.S57.ToString(), ExchangeSetLayout.Standard.ToString())));
        }

        [Test]
        public async Task GetProductDataProductVersions_Returns_ValidData_WhenValidProductVersionsArePassed()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion(A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse())),
                    Headers = { Date = DateTime.UtcNow }
                });

            var response = await _essBuilderService.GetProductDataProductVersions(
                new ProductVersionsRequest
                {
                    ProductVersions = new List<ProductVersion>
                    {
                        new ProductVersion { ProductName = "ABC000001", EditionNumber = 31, UpdateNumber = 10 }
                    }
                }, ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Standard.ToString());

            using (Assert.EnterMultipleScope())
            {
                Assert.That(!string.IsNullOrEmpty(response?.Links?.ExchangeSetFileUri?.Href), Is.True);
                Assert.That(response?.RequestedProductsNotInExchangeSet, Is.Null);
            }

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void GetProductDataProductVersions_LogsError_When_Response_Status_Is_Not_Ok()
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion(A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotModified,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StringContent(JsonConvert.SerializeObject(GetValidExchangeSetGetBatchResponse()))
                });

            Assert.ThrowsAsync<FulfilmentException>((Func<Task>)(async () => await _essBuilderService.GetProductDataProductVersions(
                new ProductVersionsRequest
                {
                    ProductVersions = new List<ProductVersion>
                    {
                        new ProductVersion { ProductName = "ABC000001", EditionNumber = 31, UpdateNumber = 10 }
                    }
                }, ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Large.ToString())));

            A.CallTo(() => _fakeAuthTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void PostProductIdentifiersData_Returns_FulfilmentException_On_Error_Status(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeEssApiClient.PostProductIdentifiersDataAsync(A<string>.Ignored, A<List<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                (Func<Task>)(async () => await _essBuilderService.PostProductIdentifiersData(new List<string> { }, ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Standard.ToString())));
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void GetProductDataSinceDateTime_Returns_FulfilmentException_On_Error_Status(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataSinceDateTime(A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                (Func<Task>)(async () => await _essBuilderService.GetProductDataSinceDateTime(DateTime.UtcNow.ToString("R"), ExchangeSetStandard.S57.ToString(), ExchangeSetLayout.Large.ToString())));
        }

        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        public void GetProductDataProductVersions_Returns_FulfilmentException_On_Error_Status(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => _fakeEssApiClient.GetProductDataProductVersion(A<string>.Ignored, A<List<ProductVersion>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://test.com") },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                (Func<Task>)(async () =>
                {
                    await _essBuilderService.GetProductDataProductVersions(new ProductVersionsRequest
                    {
                        ProductVersions = new List<ProductVersion>
                        {
                            new ProductVersion { ProductName = "ABC000001", EditionNumber = 3, UpdateNumber = 10 }
                        }
                    }, ExchangeSetStandard.S63.ToString(), ExchangeSetLayout.Standard.ToString());
                }));
        }

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = GetProductIdentifiers().Count,
            RequestedProductCount = GetProductIdentifiers().Count,
            Links = new Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = "http://test1.com" },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = "http://test2.com" },
                ExchangeSetFileUri = new LinkSetFileUri { Href = "http://test3.com" }
            }
        };

        private ExchangeSetResponseModel GetInvalidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = 0,
            RequestedProductCount = GetProductIdentifiers().Count,
            Links = new Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri { Href = "http://test1.com" },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri { Href = "http://test2.com" },
                ExchangeSetFileUri = new LinkSetFileUri { Href = "http://test3.com" }
            },
            RequestedProductsNotInExchangeSet = GetRequestedProductsNotInExchangeSet()
        };

        private IEnumerable<RequestedProductsNotInExchangeSet> GetRequestedProductsNotInExchangeSet()
        {
            return new[]
            {
                new RequestedProductsNotInExchangeSet { ProductName = "1US2ARCGD", Reason = "invalidProduct" }
            };
        }

        private List<string> GetProductIdentifiers()
        {
            return new List<string> { "US2ARCGD", "CA379151", "DE110000" };
        }
    }
}
