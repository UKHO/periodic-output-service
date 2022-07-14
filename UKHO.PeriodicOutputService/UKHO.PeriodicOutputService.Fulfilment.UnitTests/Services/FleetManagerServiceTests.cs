using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FleetManagerServiceTests
    {
        private IOptions<FleetManagerB2BApiConfiguration> _fakeFleetManagerB2BApiConfig;
        private IFleetManagerApiClient _fakeFleetManagerClient;
        private IFleetManagerService _fakeFleetManagerService;
        private ILogger<FleetManagerService> _fakeLogger;

        [SetUp]
        public void Setup()
        {
            _fakeFleetManagerB2BApiConfig = Options.Create(new FleetManagerB2BApiConfiguration() { BaseUrl = "https://test/api", UserName = "TestUser", Password = "TestPassword", SubscriptionKey = "TestSubscriptionKey" });
            _fakeFleetManagerClient = A.Fake<IFleetManagerApiClient>();
            _fakeLogger = A.Fake<ILogger<FleetManagerService>>();

            _fakeFleetManagerService = new FleetManagerService(_fakeFleetManagerB2BApiConfig, _fakeFleetManagerClient, _fakeLogger);
        }

        [Test]
        public async Task DoesGetJwtAuthUnpToken_Returns_Token_WhenValidCredentialsPassed()
        {
            A.CallTo(() => _fakeFleetManagerClient.GetJwtAuthUnpToken(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk0ifQ.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234\",\"expiration\":\"2022-06-15T16:02:52Z\"}")))
                });

            FleetMangerGetAuthTokenResponse result = await _fakeFleetManagerService.GetJwtAuthUnpToken();

            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.AuthToken, Is.Not.Null);
            });
            Assert.That(result.AuthToken, Is.EqualTo("eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk0ifQ.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234"));
        }

        [Test]
        public async Task DoesGetJwtAuthUnpToken_Returns_403_WhenInvalidCredentialsPassed()
        {
            A.CallTo(() => _fakeFleetManagerClient.GetJwtAuthUnpToken(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Forbidden")))
                });

            FleetMangerGetAuthTokenResponse result = await _fakeFleetManagerService.GetJwtAuthUnpToken();
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(result.AuthToken, Is.Empty);
            });
        }

        [Test]
        public async Task DoesGetCatalogue_Returns_200_WhenValidAccessTokenPassed()
        {
            A.CallTo(() => _fakeFleetManagerClient.GetCatalogue(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n<?xml-stylesheet type='text/xsl' href='UKHOCatalogueView.xslt'?>\r\n<UKHOCatalogueFile SchemaVersion=\"2.0.4.6\">\r\n  <BaseFileMetadata>\r\n    <MD_FileIdentifier>2019141</MD_FileIdentifier>\r\n    <MD_CharacterSet></MD_CharacterSet>\r\n    <MD_PointOfContact>\r\n      <ResponsibleParty>\r\n        <organisationName>The United Kingdom Hydrographic Office</organisationName>\r\n        <contactInfo>\r\n          <fax>+44 (0)1823 284077</fax>\r\n          <phone>+44 (0)1823 337900</phone>\r\n          <address>\r\n            <deliveryPoint>Admiralty Way</deliveryPoint>\r\n            <city>Taunton</city>\r\n            <administrativeArea>IMT</administrativeArea>\r\n            <postalCode>TA1 2DN</postalCode>\r\n            <country>United Kingdom</country>\r\n            <electronicMailAddress>helpdesk@ukho.gov.uk</electronicMailAddress>\r\n          </address>\r\n        </contactInfo>\r\n      </ResponsibleParty>\r\n    </MD_PointOfContact>\r\n    <MD_DateStamp>2019-04-02</MD_DateStamp>\r\n    <MD_StandardName></MD_StandardName>\r\n    <MD_StandardVersion></MD_StandardVersion>\r\n  </BaseFileMetadata>\r\n  <Products>\r\n    <ENC>\r\n        <ShortName>AR201010</ShortName>\r\n        <Metadata>\r\n          <DatasetTitle>Río de la Plata medio y superior</DatasetTitle>\r\n          <Scale>350000</Scale>\r\n          <GeographicLimit>\r\n            <BoundingBox>\r\n              <NorthLimit>-33.8333333</NorthLimit>\r\n              <SouthLimit>-36.4333333</SouthLimit>\r\n              <EastLimit>-54.9916667</EastLimit>\r\n              <WestLimit>-58.9</WestLimit>\r\n            </BoundingBox>\r\n            <Polygon>\r\n              <Position latitude=\"-36.43333\" longitude=\"-56.9428\" />\r\n            </Polygon>\r\n          </GeographicLimit>\r\n          <Folio>\r\n            <ID>ATLSW</ID>\r\n          </Folio>\r\n          <Folio>\r\n            <ID>PAYSF</ID>\r\n          </Folio>\r\n          <SAP_IPN>99085</SAP_IPN>\r\n          <CatalogueNumber>AR201010</CatalogueNumber>\r\n          <Status>\r\n            <ChartStatus date=\"2019-02-12\">Base</ChartStatus>\r\n            <ReplacesList>\r\n              <Replaces date=\"2019-02-12\">AR201130</Replaces>\r\n            </ReplacesList>\r\n          </Status>\r\n          <Unit>\r\n            <ID>AR201010</ID>\r\n          </Unit>\r\n          <DSNM>AR201010</DSNM>\r\n          <Usage>2</Usage>\r\n          <Edtn>1</Edtn>\r\n          <Base_isdt>2019-02-12</Base_isdt>\r\n          <UPDN>0</UPDN>\r\n          <Last_reissue_UPDN>0</Last_reissue_UPDN>\r\n          <CD>\r\n            <Base>8</Base>\r\n            <Update>0</Update>\r\n          </CD>\r\n        </Metadata>\r\n      </ENC>\r\n      \r\n  </Products>\r\n</UKHOCatalogueFile>")))
                });

            FleetManagerGetCatalogueResponse result = await _fakeFleetManagerService.GetCatalogue("JwtAuthJwtAccessToken");
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.ProductIdentifiers, Is.Not.Null);
            });
            Assert.That(result.ProductIdentifiers, Does.Contain("AR201010"));
        }

        [Test]
        public async Task DoesGetCatalogue_Returns_403_WhenInvalidAccessTokenPassed()
        {
            A.CallTo(() => _fakeFleetManagerClient.GetCatalogue(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.Forbidden,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Forbidden")))
                });

            FleetManagerGetCatalogueResponse result = await _fakeFleetManagerService.GetCatalogue("InvalidJwtAuthJwtAccessToken");
            Assert.Multiple(() =>
            {
                Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
                Assert.That(result.ProductIdentifiers, Is.Empty);
            });
        }
    }
}
