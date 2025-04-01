using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Services
{
    [TestFixture]
    public class SalesCatalogueServiceTests
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private IOptions<SalesCatalogueConfiguration> fakeSaleCatalogueConfig;
        private IAuthScsTokenProvider fakeAuthScsTokenProvider;
        private ISalesCatalogueClient fakeSalesCatalogueClient;
        private ISalesCatalogueService salesCatalogueService;
        private const string NotRequiredAccessToken = "xyz";

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            fakeSaleCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { BaseUrl = "baseSCSUrl", ProductType = "Test", Version = "t1", CatalogueType = "essTest" });
            fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            salesCatalogueService = new SalesCatalogueService(fakeLogger, fakeSaleCatalogueConfig, fakeAuthScsTokenProvider, fakeSalesCatalogueClient);
        }

        #region GetSalesCatalogueDataProductResponse
        private List<SalesCatalogueDataProductResponse> GetSalesCatalogueDataProductResponse()
        {
            return
                new List<SalesCatalogueDataProductResponse>()
                {
                    new ()
                    {
                    ProductName = "10000002",
                    LatestUpdateNumber = 5,
                    FileSize = 600,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 119,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitEasternmostLatitude = 120,
                    BaseCellEditionNumber = 3,
                    BaseCellLocation = "M0;B0",
                    BaseCellIssueDate = DateTime.Today,
                    BaseCellUpdateNumber = 0,
                    Encryption = true,
                    CancelledCellReplacements = new List<string>() { },
                    Compression = true,
                    IssueDateLatestUpdate = DateTime.Today,
                    LastUpdateNumberPreviousEdition = 0,
                    TenDataCoverageCoordinates = ",,,,,,,,,,,,,,,,,,,",
                    }
                };
        }
        #endregion

        #region GetSalesCatalogueDataResponse
        [Test]
        [TestCase(HttpStatusCode.BadRequest, "BadRequest")]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.Forbidden, "Forbidden")]
        public void WhenSCSClientReturnsOtherThanStatusCode200_ThenGetSalesCatalogueDataResponseReturnsFulfilmentException(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns(NotRequiredAccessToken);
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = statusCode, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                 async delegate { await salesCatalogueService.GetSalesCatalogueData(); });

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataRequestStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get catalogue data from SCS started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataNonOkResponse.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to Sales Catalogue Service catalogue end point with uri:{RequestUri} FAILED.| {DateTime} | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenSCSClientReturnsStatusCode200_ThenGetSalesCatalogueDataResponseReturnsStatusCode200AndDataInResponse()
        {
            List<SalesCatalogueDataProductResponse> scsResponse = GetSalesCatalogueDataProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns(NotRequiredAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await salesCatalogueService.GetSalesCatalogueData();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.ResponseCode, Is.GreaterThanOrEqualTo(HttpStatusCode.OK));
                Assert.That(JsonConvert.SerializeObject(response.ResponseBody), Is.EqualTo(jsonString));
            }

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataRequestStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get catalogue data from SCS started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataRequestCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get catalogue data from SCS completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenGetSalesCatalogueDataResponseCallsApi_ThenValidateCorrectParametersArePassed()
        {
            string actualAccessToken = "notRequiredDuringTesting";
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            string? accessTokenParam = null;
            string? uriParam = null;
            HttpMethod? httpMethodParam = null;
            var scsResponse = new List<SalesCatalogueDataResponse>();
            var jsonString = JsonConvert.SerializeObject(scsResponse);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns(actualAccessToken);
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                    postBodyParam = postBody;
                })
                .Returns(httpResponse);

            var response = await salesCatalogueService.GetSalesCatalogueData();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(httpMethodParam, Is.EqualTo(HttpMethod.Get));
                Assert.That(uriParam, Is.EqualTo($"{fakeSaleCatalogueConfig.Value.BaseUrl}/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/catalogue/{fakeSaleCatalogueConfig.Value.CatalogueType}"));
                Assert.That(accessTokenParam, Is.EqualTo(actualAccessToken));
            }

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataRequestStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get catalogue data from SCS started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsGetSalesCatalogueDataRequestCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Get catalogue data from SCS completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }
        #endregion GetSalesCatalogueDataResponse

        #region PostProductVersionsAsync
        [Test]
        public async Task WhenPostProductVersionsAsyncIsCalled_ThenReturnsExpectedResponse()
        {
            var actualAccessToken = "notRequiredDuringTesting";
            string? accessTokenParam = null;
            string? uriParam = null;
            HttpMethod? httpMethodParam = null;


            var productVersions = new List<ProductVersion>
            {
                new() { ProductName = "TestProduct", EditionNumber = 1, UpdateNumber = 1 }
            };

            var expectedResponse = new SalesCatalogueResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new SalesCatalogueProductResponse
                {
                    Products = new List<Products>
                    {
                        new()
                            {
                                ProductName = "TestProduct", EditionNumber = 1,
                                UpdateNumbers = new List<int?> {0, 1}, FileSize = 10
                            }
                    }
                }
            };

            var jsonResponse = JsonConvert.SerializeObject(expectedResponse.ResponseBody);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns(actualAccessToken);
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Invokes((HttpMethod method, string postBody, string accessToken, string uri) =>
                {
                    accessTokenParam = accessToken;
                    uriParam = uri;
                    httpMethodParam = method;
                })
                .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.OK, Content = new StringContent(jsonResponse) });

            var response = await salesCatalogueService.PostProductVersionsAsync(productVersions);

            Assert.That(response.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
            Assert.That(JsonConvert.SerializeObject(response.ResponseBody), Is.EqualTo(jsonResponse));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(response.ResponseCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(httpMethodParam, Is.EqualTo(HttpMethod.Post));
                Assert.That(uriParam, Is.EqualTo($"{fakeSaleCatalogueConfig.Value.BaseUrl}/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/products/productVersions"));
                Assert.That(accessTokenParam, Is.EqualTo(actualAccessToken));
            }

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsPostProductVersionsRequestStart.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post SCS for ProductVersions started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsPostProductVersionsRequestCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post SCS for ProductVersions completed | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenPostProductVersionsAsyncReturnsNonSuccessStatusCode_ThenThrowsFulfilmentException()
        {
            var productVersions = new List<ProductVersion>
            {
                new() { ProductName = "TestProduct", EditionNumber = 1, UpdateNumber = 1 }
            };

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored, A<string>.Ignored)).Returns(NotRequiredAccessToken);
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage { RequestUri = new Uri("http://abc.com") }, Content = new StringContent("BadRequest") });

            var response = await salesCatalogueService.PostProductVersionsAsync(productVersions);

            Assert.That(response.ResponseCode, Is.Not.EqualTo(HttpStatusCode.OK));

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.ScsPostProductVersionsRequestStart.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Post SCS for ProductVersions started | {DateTime} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.SalesCatalogueServiceNonOkResponse.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error in sales catalogue service with uri:{RequestUri} and responded with {StatusCode} and _X-Correlation-ID:{CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }
        #endregion PostProductVersionsAsync
    }
}
