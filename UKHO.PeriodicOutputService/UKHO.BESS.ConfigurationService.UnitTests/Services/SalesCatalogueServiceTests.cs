using System.Net;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService.UnitTests.Services
{
    public class SalesCatalogueServiceTests
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private IOptions<SalesCatalogueConfiguration> fakeSaleCatalogueConfig;
        private IAuthScsTokenProvider fakeAuthScsTokenProvider;
        private ISalesCatalogueClient fakeSalesCatalogueClient;
        private ISalesCatalogueService salesCatalogueService;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
            fakeAuthScsTokenProvider = A.Fake<IAuthScsTokenProvider>();
            fakeSaleCatalogueConfig = Options.Create(new SalesCatalogueConfiguration() { ProductType = "Test", Version = "t1", CatalogueType = "essTest" });
            fakeSalesCatalogueClient = A.Fake<ISalesCatalogueClient>();
            salesCatalogueService = new SalesCatalogueService(fakeLogger, fakeSaleCatalogueConfig, fakeAuthScsTokenProvider, fakeSalesCatalogueClient);
        }

        #region GetSalesCatalogueDataProductResponse
        private List<SalesCatalogueDataProductResponse> GetSalesCatalogueDataProductResponse()
        {
            return
                new List<SalesCatalogueDataProductResponse>()
                {
                    new SalesCatalogueDataProductResponse()
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
        public void WhenSCSClientReturnsStatusCodeOtherThan200_ThenGetSalesCatalogueDataResponseReturnsFulfilmentException()
        {
            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage() { StatusCode = HttpStatusCode.BadRequest, RequestMessage = new HttpRequestMessage() { RequestUri = new Uri("http://abc.com") }, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Bad request"))) });

            Assert.ThrowsAsync(Is.TypeOf<FulfilmentException>(),
                 async delegate { await salesCatalogueService.GetSalesCatalogueData(); });
        }

        [Test]
        public async Task WhenSCSClientReturnsStatusCode200_ThenGetSalesCatalogueDataResponseReturnsStatusCode200AndDataInResponse()
        {
            List<SalesCatalogueDataProductResponse> scsResponse = GetSalesCatalogueDataProductResponse();
            var jsonString = JsonConvert.SerializeObject(scsResponse);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns("notRequiredDuringTesting");
            var httpResponse = new HttpResponseMessage() { StatusCode = HttpStatusCode.OK, Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(jsonString))) };
            A.CallTo(() => fakeSalesCatalogueClient.CallSalesCatalogueServiceApi(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(httpResponse);

            var response = await salesCatalogueService.GetSalesCatalogueData();

            response.ResponseCode.Should().Be(HttpStatusCode.OK);
            JsonConvert.SerializeObject(response.ResponseBody).Should().Be(jsonString);
        }

        [Test]
        public async Task WhenGetSalesCatalogueDataResponseCallsApi_ThenValidateCorrectParametersArePassed()
        {
            string actualAccessToken = "notRequiredDuringTesting";
            string postBodyParam = "This should be replace by actual value when param passed to api call";

            string accessTokenParam = null;
            string uriParam = null;
            HttpMethod httpMethodParam = null;
            var scsResponse = new List<SalesCatalogueDataResponse>();
            var jsonString = JsonConvert.SerializeObject(scsResponse);

            A.CallTo(() => fakeAuthScsTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored)).Returns(actualAccessToken);
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

            response.ResponseCode.Should().Be(HttpStatusCode.OK);
            httpMethodParam.Should().Be(HttpMethod.Get);
            uriParam.Should().Be($"/{fakeSaleCatalogueConfig.Value.Version}/productData/{fakeSaleCatalogueConfig.Value.ProductType}/catalogue/{fakeSaleCatalogueConfig.Value.CatalogueType}");
            accessTokenParam.Should().Be(actualAccessToken);
        }
        #endregion GetSalesCatalogueDataResponse
    }
}
