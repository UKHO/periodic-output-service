using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTests
    {
        public IFulfilmentDataService _fulfilmentDataService;
        public IFleetManagerService _fakeFleetManagerService;
        public IEssService _fakeExchangeSetApiService;
        public IFssService _fakeFssBatchService;
        public IFileSystemHelper _fakefileSystemHelper;

        private ILogger<FulfilmentDataService> _fakeLogger;

        public FleetMangerGetAuthTokenResponse jwtauthUnpToken = new();
        public FleetMangerGetAuthTokenResponse jwtAuthJwtToken = new();

        [SetUp]
        public void Setup()
        {
            _fakeFleetManagerService = A.Fake<IFleetManagerService>();
            _fakeExchangeSetApiService = A.Fake<IEssService>();
            _fakeFssBatchService = A.Fake<IFssService>();
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakefileSystemHelper = A.Fake<IFileSystemHelper>();

            _fulfilmentDataService = new FulfilmentDataService(_fakeFleetManagerService, _fakeExchangeSetApiService, _fakeFssBatchService, _fakefileSystemHelper, _fakeLogger);
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

            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns(jwtauthUnpToken);

            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns(fleetManagerGetCatalogue);

            A.CallTo(() => _fakeExchangeSetApiService.PostProductIdentifiersData(A<List<string>>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            string result = await _fulfilmentDataService.CreatePosExchangeSet();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("Success"));

            A.CallTo(() => _fakeFssBatchService.CheckIfBatchCommitted(A<string>.Ignored))
              .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Check_If_GetJwtAuthUnpToken_IsNull()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.Unauthorized;
            jwtauthUnpToken.AuthToken = "";

            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken()).Returns(jwtauthUnpToken);

            string result = await _fulfilmentDataService.CreatePosExchangeSet();

            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("Fail"));
        }

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = 3,
            RequestedProductCount = 3,
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
    }
}
