using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FleetManagerServiceTest
    {
        private IOptions<FleetManagerB2BApiConfiguration> fakeFleetManagerB2BApiConfig;
        private IFleetManagerClient fakeFleetManagerClient;
        private FleetManagerService fakeFleetManagerService;


        [SetUp]
        public void Setup()
        {
            fakeFleetManagerB2BApiConfig = Options.Create(new FleetManagerB2BApiConfiguration() { BaseUrl = "https://test/api", UserName = "TestUser", Password = "TestPassword", SubscriptionKey = "TestSubscriptionKey" });
            fakeFleetManagerClient = A.Fake<IFleetManagerClient>();

            fakeFleetManagerService = new FleetManagerService(fakeFleetManagerB2BApiConfig, fakeFleetManagerClient);
        }

        [Test]
        public async Task DoesGetJwtAuthUnpToken_Returns_Unauthorised_WhenInvalidCredentialsPassed()
        {
            string base64Credentials = CommonHelper.GetBase64EncodedCredentials(fakeFleetManagerB2BApiConfig.Value.UserName, fakeFleetManagerB2BApiConfig.Value.UserName);

            A.CallTo(() => fakeFleetManagerClient.GetJwtAuthUnpToken(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.Unauthorized,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("Unauthoriesd")))
                });

            var result = await fakeFleetManagerService.GetJwtAuthUnpToken();
            Assert.That(result, Is.EqualTo(""));
        }

        [Test]
        public async Task DoesGetJwtAuthUnpToken_Returns_Token_WhenValidCredentialsPassed()
        {
            A.CallTo(() => fakeFleetManagerClient.GetJwtAuthUnpToken(A<HttpMethod>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk0ifQ.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234\",\"expiration\":\"2022-06-15T16:02:52Z\"}")))
                });

            var result = await fakeFleetManagerService.GetJwtAuthUnpToken();
            Assert.That(result, Is.Not.Null);
#pragma warning disable NUnit2021 // Incompatible types for EqualTo constraint
            Assert.That(result, Is.EqualTo("eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk0ifQ.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234"));
#pragma warning restore NUnit2021 // Incompatible types for EqualTo constraint
        }
    }
}
