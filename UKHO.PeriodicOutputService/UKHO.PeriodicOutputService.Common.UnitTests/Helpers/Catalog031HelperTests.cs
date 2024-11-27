using FakeItEasy;
using FluentAssertions;
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
    public class Catalog031HelperTests
    {
        private IOptions<FssApiConfiguration> _fakeFssApiConfiguration;
        private ICatalog031Helper _catalog031Helper;
        private IFileSystemHelper _fakeFileSystemHelper;
        private ICatalog031Builder _fakeCatalogBuilder;
        private ICatalog031Reader _fakeCatalogReader;
        private IFactory<ICatalog031Builder> _fakeCatalog031BuilderFactory;
        private Common.Helpers.ICatalog031ReaderFactory _fakeCatalog031ReaderFactory;
        private ILogger<Catalog031Helper> _fakeLogger;

        private readonly string _catalogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData");

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

            _fakeFileSystemHelper = A.Fake<IFileSystemHelper>();
            _fakeCatalog031BuilderFactory = A.Fake<IFactory<ICatalog031Builder>>();
            _fakeCatalogBuilder = A.Fake<ICatalog031Builder>();
            _fakeCatalog031ReaderFactory = A.Fake<Common.Helpers.ICatalog031ReaderFactory>();
            _fakeCatalogReader = A.Fake<ICatalog031Reader>();
            _fakeLogger = A.Fake<ILogger<Catalog031Helper>>();

            A.CallTo(() => _fakeCatalog031BuilderFactory.Create()).Returns(_fakeCatalogBuilder);

            _catalog031Helper = new Catalog031Helper(
                _fakeFileSystemHelper,
                _fakeFssApiConfiguration,
                _fakeCatalog031BuilderFactory,
                _fakeCatalog031ReaderFactory,
                _fakeLogger
            );
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullFileSystemHelper = () =>
                new Catalog031Helper(null, _fakeFssApiConfiguration, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory, _fakeLogger);
            nullFileSystemHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("fileSystemHelper");

            Action nullFssApiConfiguration = () =>
                new Catalog031Helper(_fakeFileSystemHelper, null, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory, _fakeLogger);
            nullFssApiConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should()
                .Be("fssApiConfig");

            Action nullCatalog031BuilderFactory = () =>
                new Catalog031Helper(_fakeFileSystemHelper, _fakeFssApiConfiguration, null, _fakeCatalog031ReaderFactory, _fakeLogger);
            nullCatalog031BuilderFactory.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("catalog031BuilderFactory");

            Action nullCatalog031ReaderFactory = () =>
                new Catalog031Helper(_fakeFileSystemHelper, _fakeFssApiConfiguration, _fakeCatalog031BuilderFactory, null, _fakeLogger);
            nullCatalog031ReaderFactory.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("catalog031ReaderFactory");

            Action nullLogger = () =>
                new Catalog031Helper(_fakeFileSystemHelper, _fakeFssApiConfiguration, _fakeCatalog031BuilderFactory, _fakeCatalog031ReaderFactory, null);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");
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

            _catalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(_catalogFilePath);

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

            _catalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(_catalogFilePath);

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

            _catalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(_catalogFilePath);

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
            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(A<string>.Ignored))
                .Throws(new Exception("An error occurred while deleting the catalog file."));

            Action action = () => _catalog031Helper.RemoveReadmeEntryAndUpdateCatalogFile(_catalogFilePath);

            action.Should().Throw<Exception>().WithMessage("An error occurred while deleting the catalog file.");
            A.CallTo(_fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                & call.GetArgument<EventId>(1) ==
                EventIds.RemoveReadMeEntryAndUpdateCatalogFileProcessFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)
                    ["{OriginalFormat}"].ToString() ==
                "An error occurred while processing catalog file. | ErrorMessage: {ErrorMessage} | _X-Correlation-ID: {CorrelationId}"
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
                "Starting the process of removing README entry and updating catalog file. | _X-Correlation-ID : {CorrelationId}"
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
                "Successfully completed the process of removing README entry and updating catalog for file. | _X-Correlation-ID : {CorrelationId}"
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
            A.CallTo(() => _fakeFileSystemHelper.DeleteFile(A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileSystemHelper.CreateFileCopy(A<string>.Ignored, A<MemoryStream>._))
                .MustHaveHappenedOnceExactly();
        }
    }
}
