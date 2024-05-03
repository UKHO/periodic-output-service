using System.IO.Abstractions;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.BESS.CleanUpJob.Configuration;
using UKHO.BESS.CleanUpJob.Services;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob.UnitTests.Services
{
    [TestFixture]
    public class BespokeExchangeSetCleanUpServiceTests
    {
        private BespokeExchangeSetCleanUpService bespokeExchangeSetCleanUpService;
        private IFileSystem fakeFileSystem;
        private ILogger<BespokeExchangeSetCleanUpService> fakeLogger;
        private IOptions<CleanUpConfiguration> fakeCleanUpConfig;
        private IConfiguration fakeConfiguration;
        public string[] fakeFilePath = { @"D:\\Downloads", @"D:\\test", @"D:\\test1" };
        private DateTime fakeDateTime;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<BespokeExchangeSetCleanUpService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFileSystem = A.Fake<IFileSystem>();
            fakeCleanUpConfig = Options.Create(new CleanUpConfiguration()
            { NumberOfDays = 5 });
            fakeDateTime = DateTime.UtcNow.AddDays(-6);

        bespokeExchangeSetCleanUpService = new BespokeExchangeSetCleanUpService(fakeConfiguration, fakeLogger, fakeCleanUpConfig, fakeFileSystem);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new BespokeExchangeSetCleanUpService(fakeConfiguration, null, fakeCleanUpConfig, fakeFileSystem);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCleanupConfiguration = () => new BespokeExchangeSetCleanUpService(fakeConfiguration, fakeLogger, null, fakeFileSystem);
            nullCleanupConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("cleanUpConfig");

            Action nullFileSystem = () => new BespokeExchangeSetCleanUpService(fakeConfiguration, fakeLogger, fakeCleanUpConfig, null);
            nullFileSystem.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");

            Action nullConfiguration = () => new BespokeExchangeSetCleanUpService(null, fakeLogger, fakeCleanUpConfig, fakeFileSystem);
            nullConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesFound_ThenCleanupJobIsSuccessful()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(fakeFilePath);
            A.CallTo(() => fakeFileSystem.Directory.GetLastWriteTimeUtc(A<string>.Ignored)).Returns(fakeDateTime);

            await bespokeExchangeSetCleanUpService.CleanUpHistoricFoldersAndFiles();

            A.CallTo(() => fakeFileSystem.Directory.Delete(A<string>.Ignored, A<bool>.Ignored)).MustHaveHappenedTwiceOrMore();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CleanUpSuccessful.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Successfully cleaned the folder | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenFolderPathsNotFound_ThenCleanupProcessIsNotRunned()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(new string[] {});
       
            await bespokeExchangeSetCleanUpService.CleanUpHistoricFoldersAndFiles();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.NoFoldersFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No folders to delete | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenHistoricFoldersAndFilesNotFound_ThenCleanupJobLogsError()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(fakeFilePath);
            A.CallTo(() => fakeFileSystem.Directory.GetLastWriteTimeUtc(A<string>.Ignored)).Returns(DateTime.UtcNow);

            await bespokeExchangeSetCleanUpService.CleanUpHistoricFoldersAndFiles();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.NoFoldersFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No folders to delete based on the cleanup configured date - {historicDate} | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }
    }
}
