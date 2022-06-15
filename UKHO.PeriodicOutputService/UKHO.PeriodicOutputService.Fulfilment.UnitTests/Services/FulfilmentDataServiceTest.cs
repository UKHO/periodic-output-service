using FakeItEasy;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTest
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
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123");
            A.CallTo(() => fakeFleetManagerService.GetJwtAuthJwtToken(A<string>.Ignored))
              .Returns("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ456");
            A.CallTo(() => fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns("TestCatalogueXML");

            string result = await fulfilmentDataService.CreatePosExchangeSet();

            Assert.That(result, Is.Not.Null);
        }
    }
}
