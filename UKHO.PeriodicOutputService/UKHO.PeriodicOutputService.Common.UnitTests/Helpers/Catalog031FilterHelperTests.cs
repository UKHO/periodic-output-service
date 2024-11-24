using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.Torus.Core;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class Catalog031FilterHelperTests
    {
        private IOptions<FssApiConfiguration> _fakeFssApiConfiguration;
        private ICatalog031FilterHelper _catalog031FilterHelper;
        private IFileSystemHelper _fakeFileSystemHelper;
        private IConfiguration _fakeConfiguration;
        private ICatalog031Builder _fakeCatalogBuilder;
        private ICatalog031Reader _fakeCatalogReader;
        private ILogger<Catalog031FilterHelper> _fakeLogger;
        private IFactory<ICatalog031Builder> _fakeCatalog031BuilderFactory;
        private Common.Helpers.ICatalog031ReaderFactory _fakeCatalog031ReaderFactory;

        private readonly string _catalogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Catalog.031");
        private const string EXCHANGESETCATALOGFILE = "ExchangeSetCatalogFileName";

        [SetUp]
        public void Setup()
        {
            _fakeFssApiConfiguration = Options.Create(new FssApiConfiguration()
            {
                BaseUrl = "http://test.com",
                FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                BatchStatusPollingCutoffTime = "0.1",
                BatchStatusPollingDelayTime = "20",
                BatchStatusPollingCutoffTimeForAIO = "0.1",
                BatchStatusPollingDelayTimeForAIO = "20",
                BatchStatusPollingCutoffTimeForBESS = "0.1",
                BatchStatusPollingDelayTimeForBESS = "20",
                PosReadUsers = "",
                PosReadGroups = "public",
                BlockSizeInMultipleOfKBs = 4096,
                EncRoot = "ENC_ROOT",
                ReadMeFileName = "README.TXT"
            });

            _fakeLogger = A.Fake<ILogger<Catalog031FilterHelper>>();
            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeConfiguration = A.Fake<IConfiguration>();
            _fakeCatalog031BuilderFactory = A.Fake<IFactory<ICatalog031Builder>>();
            _fakeCatalogBuilder = A.Fake<ICatalog031Builder>();
            _fakeCatalog031ReaderFactory = A.Fake<Common.Helpers.ICatalog031ReaderFactory>();
            _fakeCatalogReader = A.Fake<ICatalog031Reader>();
            _fakeConfiguration[EXCHANGESETCATALOGFILE] = "CATALOG.031";

            A.CallTo(() => _fakeCatalog031BuilderFactory.Create()).Returns(_fakeCatalogBuilder);

            _catalog031FilterHelper = new Catalog031FilterHelper(
                _fakeFileSystemHelper,
                _fakeFssApiConfiguration,
                _fakeConfiguration,
                _fakeLogger,
                _fakeCatalog031BuilderFactory,
                _fakeCatalog031ReaderFactory
            );
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullFileSystemHelper = () =>
                new Catalog031FilterHelper(null, _fakeFssApiConfiguration, _fakeConfiguration, _fakeLogger, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("fileSystemHelper");

            Action nullFssApiConfiguration = () =>
                new Catalog031FilterHelper(_fakeFileSystemHelper, null, _fakeConfiguration, _fakeLogger, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory);
            nullFssApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("fssApiConfig");

            Action nullConfiguration = () =>
                new Catalog031FilterHelper(_fakeFileSystemHelper, _fakeFssApiConfiguration, null, _fakeLogger, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory);
            nullConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");

            Action nullLogger = () =>
                new Catalog031FilterHelper(_fakeFileSystemHelper, _fakeFssApiConfiguration, _fakeConfiguration, null, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCatalog031BuilderFactory = () =>
                new Catalog031FilterHelper(_fakeFileSystemHelper, _fakeFssApiConfiguration, _fakeConfiguration, _fakeLogger, null, _fakeCatalog031ReaderFactory);
            nullCatalog031BuilderFactory.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("catalog031BuilderFactory");

            Action nullCatalog031ReaderFactory = () =>
                new Catalog031FilterHelper(_fakeFileSystemHelper, _fakeFssApiConfiguration, _fakeConfiguration, _fakeLogger, _fakeCatalog031BuilderFactory, null);
            nullCatalog031ReaderFactory.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("catalog031ReaderFactory");
        }

        [Test]
        public void WhenCatalogDoesNotContainReadMeAndCatalogEntry_ThenRemoveReadmeEntryAndUpdateCatalog_ShouldNotSkipCatalogEntriesAndUpdatesCatalog()
        {
            var catalogEntries = new List<CatalogEntry>
            {
                new() { FileLocation = "TEST.031" },
                new() { FileLocation = "TEST.TXT" }
            };
            
            A.CallTo(() => _fakeCatalogReader.ReadCatalogue()).Returns(catalogEntries);

            A.CallTo(() => _fakeCatalog031ReaderFactory.Create(A<byte[]>.Ignored)).Returns(_fakeCatalogReader);

            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(_catalogFilePath)).DoesNothing();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(_catalogFilePath, A<MemoryStream>._)).DoesNothing();

            _catalog031FilterHelper.RemoveReadmeEntryAndUpdateCatalog(_catalogFilePath);

            VerifyCatalogEntriesAdded(catalogEntries);
            VerifyCatalogFileOperations();
            VerifyLoggingStart();
            VerifyLoggingComplete();
        }

        [Test]
        public void WhenCatalogContainsReadMeFile_ThenRemoveReadmeEntryAndUpdateCatalog_ShouldSkipsReadMeEntryAndUpdatesCatalog()
        {
           var catalogEntries = new List<CatalogEntry>
           {
               new() { FileLocation = "README.TXT" },
               new() { FileLocation = "VALID.CAT" }
           };

           A.CallTo(() => _fakeCatalogReader.ReadCatalogue()).Returns(catalogEntries);

            A.CallTo(() => _fakeCatalog031ReaderFactory.Create(A<byte[]>.Ignored)).Returns(_fakeCatalogReader);

           A.CallTo(() => _fakeFileSystemHelper.DeleteFile(_catalogFilePath)).DoesNothing();
           A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(_catalogFilePath, A<MemoryStream>._)).DoesNothing();

           _catalog031FilterHelper.RemoveReadmeEntryAndUpdateCatalog(_catalogFilePath);

           VerifyCatalogEntriesAdded(new List<CatalogEntry> { catalogEntries[1] });
           A.CallTo(() => _fakeCatalogBuilder.Add(A<CatalogEntry>.That.Matches(x => x.FileLocation == "README.TXT"))).MustNotHaveHappened();
           VerifyCatalogFileOperations();
           VerifyLoggingStart();
           VerifyLoggingComplete();
        }

        [Test]
        public void WhenCatalogContainsReadMeAndCatalogEntry_ThenRemoveReadMeAndUpdateCatalog_ShouldSkipBothEntriesAndUpdatesCatalog()
        {
            var catalogEntries = new List<CatalogEntry>
           {
               new() { FileLocation = "README.TXT" },
               new() { FileLocation = "CATALOG.031" }
           };

            A.CallTo(() => _fakeCatalogReader.ReadCatalogue()).Returns(catalogEntries);

            A.CallTo(() => _fakeCatalog031ReaderFactory.Create(A<byte[]>.Ignored)).Returns(_fakeCatalogReader);

            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(_catalogFilePath)).DoesNothing();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(_catalogFilePath, A<MemoryStream>._)).DoesNothing();

            _catalog031FilterHelper.RemoveReadmeEntryAndUpdateCatalog(_catalogFilePath);

            A.CallTo(() => _fakeCatalogBuilder.Add(A<CatalogEntry>._)).MustNotHaveHappened();
            VerifyCatalogFileOperations();
            VerifyLoggingStart();
            VerifyLoggingComplete();
        }

        [Test]
        public void WhenRemoveReadmeEntryAndUpdateCatalogThrowsException_ThenExceptionIsLoggedAndRethrown()
        {
            var catalogEntries = new List<CatalogEntry>
            {
                new() { FileLocation = "VALID.CAT" }
            };

            A.CallTo(() => _fakeCatalogReader.ReadCatalogue()).Returns(catalogEntries);

            A.CallTo(() => _fakeCatalog031ReaderFactory.Create(A<byte[]>.Ignored)).Returns(_fakeCatalogReader);
            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(_catalogFilePath))
                .Throws(new Exception("An error occurred while deleting the catalog file."));

            Action action = () => _catalog031FilterHelper.RemoveReadmeEntryAndUpdateCatalog(_catalogFilePath);

            action.Should().Throw<Exception>().WithMessage("An error occurred while deleting the catalog file.");
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                & call.GetArgument<EventId>(1) ==
                EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() ==
                "An error occurred while processing catalog file: {CatalogFilePath} at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(A<string>._, A<MemoryStream>._)).MustNotHaveHappened();
        }

        private void VerifyLoggingStart()
        {
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() ==
                "Starting the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        private void VerifyLoggingComplete()
        {
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() ==
                "Successfully completed the process of removing README entry and updating catalog for file:| {CatalogFilePath} | {DateTime} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        private void VerifyCatalogEntriesAdded(List<CatalogEntry> expectedEntries)
        {
            foreach (var entry in expectedEntries)
            {
                A.CallTo(() => _fakeCatalogBuilder.Add(A<CatalogEntry>.That.Matches(x => x.FileLocation == entry.FileLocation)))
                    .MustHaveHappenedOnceExactly();
            }
        }

        private void VerifyCatalogFileOperations()
        {
            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(_catalogFilePath)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(_catalogFilePath, A<MemoryStream>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}
