using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Fulfilment.Models;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FulfilmentDataServiceTests
    {
        private IFulfilmentDataService _fulfilmentDataService;
        private IFleetManagerService _fakeFleetManagerService;
        private IEssService _fakeEssService;
        private IFssService _fakeFssService;
        private ILogger<FulfilmentDataService> _fakeLogger;
        private IFileSystemHelper _fakefileSystemHelper;
        private IConfiguration _fakeconfiguration;

        public FleetMangerGetAuthTokenResponseModel jwtauthUnpToken = new();
        public FleetMangerGetAuthTokenResponseModel jwtAuthJwtToken = new();

        [SetUp]
        public void Setup()
        {
            _fakeFleetManagerService = A.Fake<IFleetManagerService>();
            _fakeEssService = A.Fake<IEssService>();
            _fakeFssService = A.Fake<IFssService>();
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakefileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeconfiguration = A.Fake<IConfiguration>();

            _fulfilmentDataService = new FulfilmentDataService(_fakeFleetManagerService, _fakeEssService, _fakeFssService, _fakefileSystemHelper, _fakeLogger, _fakeconfiguration);
        }

        [Test]
        public async Task Does_CreatePosExchangeSets_Executes_Successfully()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
            jwtauthUnpToken.AuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123";

            FleetManagerGetCatalogueResponseModel fleetManagerGetCatalogue = new()
            {
                StatusCode = HttpStatusCode.OK,
                ProductIdentifiers = new() { "Product1", "Product2" }
            };

            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns(jwtauthUnpToken);

            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns(fleetManagerGetCatalogue);

            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored))
              .Returns(Common.Enums.FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            string result = await _fulfilmentDataService.CreatePosExchangeSets();

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.EqualTo("success"));

            A.CallTo(() => _fakefileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Check_If_GetJwtAuthUnpToken_IsNull()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
            jwtauthUnpToken.AuthToken = "";

            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken()).Returns(jwtauthUnpToken);

            string result = await _fulfilmentDataService.CreatePosExchangeSets();

            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored)).MustNotHaveHappened();

            Assert.That(result, Is.Not.Null);

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Product identifiers not found"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task Does_CreatePosExchangeSet_Check_If_GetBatchFiles_Contains_FileName_Error()
        {
            jwtauthUnpToken.StatusCode = HttpStatusCode.OK;
            jwtauthUnpToken.AuthToken = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ123";

            FleetManagerGetCatalogueResponseModel fleetManagerGetCatalogue = new()
            {
                StatusCode = HttpStatusCode.OK,
                ProductIdentifiers = new() { "Product1", "Product2" }
            };

            A.CallTo(() => _fakeFleetManagerService.GetJwtAuthUnpToken())
              .Returns(jwtauthUnpToken);

            A.CallTo(() => _fakeFleetManagerService.GetCatalogue(A<string>.Ignored))
              .Returns(fleetManagerGetCatalogue);

            A.CallTo(() => _fakeEssService.PostProductIdentifiersData(A<List<string>>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());

            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored))
              .Returns(Common.Enums.FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetBatchResponseModelWithFileNameError());

            string result = await _fulfilmentDataService.CreatePosExchangeSets();

            Assert.That(result, Is.Not.Null);

            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "ESS exchange set creation failed."
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.CreateDirectory(A<string>.Ignored))
                .MustNotHaveHappened();
        }

        private ExchangeSetResponseModel GetValidExchangeSetGetBatchResponse() => new()
        {
            ExchangeSetCellCount = 3,
            RequestedProductCount = 3,
            Links = new Models.Links
            {
                ExchangeSetBatchDetailsUri = new LinkSetBatchDetailsUri
                {
                    Href = "http://test1.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                },
                ExchangeSetBatchStatusUri = new LinkSetBatchStatusUri
                {
                    Href = "http://test2.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                },
                ExchangeSetFileUri = new LinkSetFileUri
                {
                    Href = "http://test3.com/621E8D6F-9950-4BA6-BFB4-92415369AAEE"
                }
            }
        };


        private static GetBatchResponseModel GetValidBatchResponseModel() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files = new List<BatchFile>
            {
               new BatchFile
               {
                   Filename = "M01X02.zip",
                   Links = new Common.Models.Fss.Response.Links
                   {
                       Get = new Link
                       {
                           Href ="http://test1.com"
                       }
                   }
               }
            }
        };

        private static GetBatchResponseModel GetBatchResponseModelWithFileNameError() => new()
        {
            BatchId = Guid.NewGuid().ToString(),
            Files = new List<BatchFile>
            {
               new BatchFile
               {
                   Filename = "Error.txt",
                   Links = new Common.Models.Fss.Response.Links
                   {
                       Get = new Link
                       {
                           Href ="http://test1.com"
                       }
                   }
               }
            }

        };
    }
}
