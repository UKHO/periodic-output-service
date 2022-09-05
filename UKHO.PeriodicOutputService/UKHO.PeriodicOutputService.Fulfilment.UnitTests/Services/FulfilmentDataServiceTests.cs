using System.IO.Abstractions;
using System.Net;
using FakeItEasy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
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
        private IFileInfo _fakeFileInfo;
        private IAzureTableStorageHelper _fakeAzureTableStorageHelper;

        public FleetMangerGetAuthTokenResponseModel jwtauthUnpToken = new();

        [SetUp]
        public void Setup()
        {
            _fakeFleetManagerService = A.Fake<IFleetManagerService>();
            _fakeEssService = A.Fake<IEssService>();
            _fakeFssService = A.Fake<IFssService>();
            _fakeLogger = A.Fake<ILogger<FulfilmentDataService>>();
            _fakefileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeconfiguration = A.Fake<IConfiguration>();
            _fakeFileInfo = A.Fake<IFileInfo>();
            _fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();

            _fulfilmentDataService = new FulfilmentDataService(_fakeFleetManagerService, _fakeEssService, _fakeFssService, _fakefileSystemHelper, _fakeLogger, _fakeconfiguration, _fakeAzureTableStorageHelper);
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


            A.CallTo(() => _fakeEssService.GetProductDataSinceDateTime(A<string>.Ignored))
              .Returns(GetValidExchangeSetGetBatchResponse());


            A.CallTo(() => _fakeFssService.CheckIfBatchCommitted(A<string>.Ignored))
              .Returns(Common.Enums.FssBatchStatus.Committed);

            A.CallTo(() => _fakeFssService.GetBatchDetails(A<string>.Ignored))
              .Returns(GetValidBatchResponseModel());

            A.CallTo(() => _fakefileSystemHelper.GetFiles(A<string>.Ignored, A<string>.Ignored, A<SearchOption>.Ignored))
                           .Returns(new List<string> { @"D:\Test" });

            A.CallTo(() => _fakeFssService.CreateBatch(A<string>.Ignored, A<Batch>.Ignored))
                           .Returns(Guid.NewGuid().ToString());

            A.CallTo(() => _fakeFileInfo.Name).Returns("M01X01.zip");
            A.CallTo(() => _fakeFileInfo.Length).Returns(100990);

            A.CallTo(() => _fakefileSystemHelper.GetFileInfo(A<string>.Ignored))
                          .Returns(_fakeFileInfo);

            A.CallTo(() => _fakeFssService.AddFileToBatch(A<string>.Ignored, A<string>.Ignored, A<long>.Ignored))
                .Returns(true);

            A.CallTo(() => _fakeFssService.UploadBlocks(A<string>.Ignored, A<IFileInfo>.Ignored))
                .Returns(new List<string> { "Block_00001" });

            A.CallTo(() => _fakeFssService.CommitBatch(A<string>.Ignored, A<IEnumerable<string>>.Ignored))
              .Returns(true);

            bool result = await _fulfilmentDataService.CreatePosExchangeSets();

            Assert.That(result, Is.True);

            A.CallTo(() => _fakefileSystemHelper.CreateDirectory(A<string>.Ignored))
               .MustHaveHappenedOnceOrMore();

            A.CallTo(() => _fakeFssService.WriteBlockFile(A<string>.Ignored, A<string>.Ignored, A<IEnumerable<string>>.Ignored))
                .MustHaveHappenedOnceOrMore();
        }



        [Test]
        public void Does_CreatePosExchangeSet_Check_If_GetBatchFiles_Contains_FileName_Error()
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

            Assert.ThrowsAsync<FulfilmentException>(
                () => _fulfilmentDataService.CreatePosExchangeSets());



            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Either no files found or error file found in batch with BathcID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.CreateDirectory(A<string>.Ignored))
                .MustHaveHappened();
        }

        [Test]
        public void Does_CreatePosExchangeSet_Check_If_ExtractExchangeSetZip_Throws_Error()
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

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            Assert.ThrowsAsync<AggregateException>(
                () => _fulfilmentDataService.CreatePosExchangeSets());


            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.CreateIsoAndSha1(A<string>.Ignored, A<string>.Ignored))
                .MustNotHaveHappened();


        }

        [Test]
        public void Does_CreatePosExchangeSet_Check_If_FulfilmentException_Thrown_When_BatchStatus_IsNot_Committed()
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
              .Returns(Common.Enums.FssBatchStatus.CommitInProgress);


            Assert.ThrowsAsync<FulfilmentException>(
                () => _fulfilmentDataService.CreatePosExchangeSets());


            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

        }

        [Test]
        public void Does_CreatePosExchangeSet_Check_If_CreateIsoAndSha1_Throws_Error()
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

            A.CallTo(() => _fakefileSystemHelper.CreateIsoAndSha1(A<string>.Ignored, A<string>.Ignored)).Throws<Exception>();

            Assert.ThrowsAsync<AggregateException>(
                () => _fulfilmentDataService.CreatePosExchangeSets());


            A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Creating ISO and Sha1 file of {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(() => _fakefileSystemHelper.ExtractZipFile(A<string>.Ignored, A<string>.Ignored, A<bool>.Ignored))
                .MustHaveHappenedOnceExactly();
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
            },
            ResponseDateTime = DateTime.UtcNow
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
