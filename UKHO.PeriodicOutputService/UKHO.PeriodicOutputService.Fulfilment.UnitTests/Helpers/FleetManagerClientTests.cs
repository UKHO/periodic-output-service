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
    public class FleetManagerClientTests
    {
        private IFleetManagerClient _fleetManagerClient;
        private IHttpClientFactory _fakeHttpClientFactory;
        private IHttpClientFacade _fakeHttpClientFacade;
        [SetUp]
        public void Setup()
        {
            _fakeHttpClientFactory = A.Fake<IHttpClientFactory>();
            _fakeHttpClientFacade = A.Fake<IHttpClientFacade>();

            A.CallTo(() => _fakeHttpClientFactory.CreateClient(true)).Returns(_fakeHttpClientFacade);

            _fleetManagerClient = new FleetManagerClient(_fakeHttpClientFactory);
        }

        [Test]
        public void DoesGetJwtAuthUnpToken_Returns_OK()
        {
            string AuthToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6IjJaUXBKM1VwYmpBWVh";

            var response = new HttpResponseMessage()
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(AuthToken)
            };

            A.CallTo(() => _fakeHttpClientFacade.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored)).Returns(response);


            var result = _fleetManagerClient.GetJwtAuthUnpToken(HttpMethod.Get, "http://test.com", "credentials", "asdfsa");

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(result.Result.Content.ReadAsStringAsync().Result, Is.EqualTo(AuthToken));

            });
        }

        [Test]
        public void DoesGetCatalogue_Returns_OK()
        {
            var serializedProductIdentifier = JsonConvert.SerializeObject(GetProductIdentifiers());

            var response = new HttpResponseMessage(HttpStatusCode.OK);
            response.Content = new StringContent(JsonConvert.SerializeObject(GetProductIdentifiers()));

            A.CallTo(() => _fakeHttpClientFacade.SendAsync(A<HttpRequestMessage>.Ignored, A<CancellationToken>.Ignored)).Returns(response);

            var result = _fleetManagerClient.GetCatalogue(HttpMethod.Get, "http://test.com", "credentials", "asdfsa");

            var deSerializedResult = JsonConvert.DeserializeObject<List<string>>(result.Result.Content.ReadAsStringAsync().Result);

            Assert.Multiple(() =>
            {
                Assert.That(result.Result.StatusCode, Is.EqualTo(HttpStatusCode.OK));
                Assert.That(deSerializedResult.Count, Is.EqualTo(GetProductIdentifiers().Count));

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
    }
}
