using FakeItEasy;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTests
    {
        public FulfilmentDataService fulfilmentDataService;
        public IFleetManagerService fakeFleetManagerService;

        [SetUp]
        public void Setup()
        {
            fakeFleetManagerService = A.Fake<IFleetManagerService>();
            fulfilmentDataService = new FulfilmentDataService(fakeFleetManagerService);
        }

        [Test]
        public async Task WhenPOSWebjobTrigger_ThenCallFleetManagerAndGetFullAVCSCatalogueXMLSuccessfully()
        {
            List<string> productIdentifiers = new() { "Product1", "Product2" };
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123");
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthJwtToken(A<string>.Ignored))
              .Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ456");
            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns(productIdentifiers);

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            Assert.That(result, Is.Not.Null);
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Skip_GetJwtAuthJwtToken_And_GetCatalogue_If_GetJwtAuthUnpToken_IsNull()
        {
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken()).Returns("");

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            A.CallTo(() => fakeFleetManagerService.GetJwtAuthJwtToken(A<string>.Ignored)).MustNotHaveHappened();
            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Skip_GetCatalogue_If_GetJwtAuthJwtToken_IsNull()
        {
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken()).Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123");
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthJwtToken(A<string>.Ignored)).Returns("");

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();
        }
    }
}
