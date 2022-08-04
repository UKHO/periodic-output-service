////using System.Net;
////using FakeItEasy;
////using Microsoft.Extensions.Logging;
////using UKHO.PeriodicOutputService.Fulfilment.Models;
////using UKHO.PeriodicOutputService.Fulfilment.Services;

////namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
////{
////    [TestFixture]
////    public class FulfilmentDataServiceTests
////    {
////        public IFulfilmentDataService _fulfilmentDataService;
////        public IFleetManagerService _fakeFleetManagerService;
////        public IEssService _fakeEssService;
////        public IFssService _fakeFssService;
////        private ILogger<FulfilmentDataService> _fakeLogger;

////        public FleetMangerGetAuthTokenResponseModel jwtauthUnpToken = new();
////        public FleetMangerGetAuthTokenResponseModel jwtAuthJwtToken = new();

////        [SetUp]
////        public void Setup()
////        {
////            _fakeFleetManagerService = A.Fake<IFleetManagerService>();
////            _fakeEssService = A.Fake<IEssService>();
////            _fakeFssService = A.Fake<IFssService>();
////            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();

////            _fulfilmentDataService = new FulfilmentDataService(_fakeFleetManagerService, _fakeEssService, _fakeFssService, _fakeLogger);
////        }

////        [Test]
////        public async Task WhenPOSWebjobTrigger_ThenCallFleetManager_GetFullAVCSCatalogueXML_And_ExtractProductIdentifiersSuccessfully()
////        {
////            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
////            jwtauthUnpToken.AuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123";

////            FleetManagerGetCatalogueResponseModel fleetManagerGetCatalogue = new()
////            {
////                StatusCode = HttpStatusCode.OK,
////                ProductIdentifiers = new() { "Product1", "Product2" }
////            };

////            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken())
////              .Returns(jwtauthUnpToken);

////            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
////              .Returns(fleetManagerGetCatalogue);

////            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored))
////              .Returns(GetValidExchangeSetGetBatchResponse());

////            string result = await _fulfilmentDataService.CreatePosExchangeSets();

////            Assert.That(result, Is.Not.Null);
////            Assert.That(result, Is.EqualTo("Success"));

////            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored))
////              .MustHaveHappenedOnceExactly();
////        }

////        [Test]
////        public async Task Does_CreatePosExchangeSet_Check_If_GetJwtAuthUnpToken_IsNull()
////        {
////            jwtauthUnpToken.StatusCode = HttpStatusCode.Unauthorized;
////            jwtauthUnpToken.AuthToken = "";

////            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken()).Returns(jwtauthUnpToken);

////            string result = await _fulfilmentDataService.CreatePosExchangeSets();

////            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();

////            Assert.That(result, Is.Not.Null);
////            Assert.That(result, Is.EqualTo("Fail"));
////        }

////        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new ExchangeSetResponseModel
////        {
////            ExchangeSetCellCount = 3,
////            RequestedProductCount = 3,
////            Links = new Links
////            {
////                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
////                {
////                    Href = "http://test1.com"
////                },
////                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
////                {
////                    Href = "http://test2.com"
////                },
////                ExchangeSetFileUri = new LinkSetFileUri
////                {
////                    Href = "http://test3.com"
////                }
////            }
////        };
////    }
////}
