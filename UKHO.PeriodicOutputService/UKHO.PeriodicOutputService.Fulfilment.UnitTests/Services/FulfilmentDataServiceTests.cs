using System.Net;
using FakeItEasy;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTests
    {
        public IFulfilmentDataService fulfilmentDataService;
        public IFleetManagerService fakeFleetManagerService;
        public IExchangeSetApiService fakeExchangeSetApiService;
        public IFssBatchService fakeFssBatchService;

        public FleetMangerGetAuthTokenResponse jwtauthUnpToken = new();
        public FleetMangerGetAuthTokenResponse jwtAuthJwtToken = new();

        [SetUp]
        public void Setup()
        {
            fakeFleetManagerService = A.Fake<IFleetManagerService>();
            fakeExchangeSetApiService = A.Fake<IExchangeSetApiService>();
            fakeFssBatchService = A.Fake<IFssBatchService>();

            fulfilmentDataService = new FulfilmentDataService(fakeFleetManagerService, fakeExchangeSetApiService, fakeFssBatchService);
        }

        [Test]
        public async Task WhenPOSWebjobTrigger_ThenCallFleetManager_GetFullAVCSCatalogueXML_And_ExtractProductIdentifiersSuccessfully()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
            jwtauthUnpToken.AuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123";

            FleetManagerGetCatalogueResponse fleetManagerGetCatalogue = new()
            {
                StatusCode = HttpStatusCode.OK,
                ProductIdentifiers = new() { "Product1", "Product2" }
            };

            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns(jwtauthUnpToken);
            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns(fleetManagerGetCatalogue);

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("Fleet Manager full AVCS ProductIdentifiers received"));
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Check_If_GetJwtAuthUnpToken_IsNull()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
            jwtauthUnpToken.AuthToken = "";

            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken()).Returns(jwtauthUnpToken);

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("Fleet Manager full AVCS ProductIdentifiers not received"));
        }
    }
}
