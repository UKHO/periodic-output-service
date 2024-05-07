using System.IO.Abstractions;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.BESS.CleanUpJob.Configuration;
using UKHO.BESS.CleanUpJob.Services;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob.UnitTests.Services
{
    [TestFixture]
    public class BessCleanUpServiceTests
    {
        private CleanUpService bessCleanUpService;
        private IFileSystem fakeFileSystem;
        private ILogger<CleanUpService> fakeLogger;
        private IOptions<CleanUpConfiguration> fakeCleanUpConfig;
        private IConfiguration fakeConfiguration;
        public string[] fakeFilePath = { @"D:\\Downloads", @"D:\\test", @"D:\\test1" };
        private DateTime fakeDateTime;

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<CleanUpService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeFileSystem = A.Fake<IFileSystem>();
            fakeCleanUpConfig = Options.Create(new CleanUpConfiguration()
            { NumberOfDays = 5 });
            fakeDateTime = DateTime.UtcNow.AddDays(-6);

            bessCleanUpService = new CleanUpService(fakeConfiguration, fakeLogger, fakeCleanUpConfig, fakeFileSystem);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullLogger = () => new CleanUpService(fakeConfiguration, null, fakeCleanUpConfig, fakeFileSystem);
            nullLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullCleanupConfiguration = () => new CleanUpService(fakeConfiguration, fakeLogger, null, fakeFileSystem);
            nullCleanupConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("cleanUpConfig");

            Action nullFileSystem = () => new CleanUpService(fakeConfiguration, fakeLogger, fakeCleanUpConfig, null);
            nullFileSystem.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("fileSystem");

            Action nullConfiguration = () => new CleanUpService(null, fakeLogger, fakeCleanUpConfig, fakeFileSystem);
            nullConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");
        }

        [Test]
        public void WhenHistoricFoldersAndFilesFound_ThenCleanupJobIsSuccessful()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(fakeFilePath);
            A.CallTo(() => fakeFileSystem.Directory.GetLastWriteTimeUtc(A<string>.Ignored)).Returns(fakeDateTime);

            var result = bessCleanUpService.CleanUpHistoricFoldersAndFiles();
            result.Should().Be("Successfully cleaned the folder");
            A.CallTo(() => fakeFileSystem.Directory.Delete(A<string>.Ignored, A<bool>.Ignored)).MustHaveHappenedTwiceOrMore();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.CleanUpSuccessful.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Successfully cleaned the folder | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenFolderPathsNotFound_ThenCleanupProcessIsNotPerformed()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(new string[] { });

            var result = bessCleanUpService.CleanUpHistoricFoldersAndFiles();
            result.Should().Be("No folders to delete");

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.NoFoldersFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No folders to delete | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenHistoricFoldersAndFilesNotFound_ThenCleanupProcessIsNotPerformed()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(fakeFilePath);
            A.CallTo(() => fakeFileSystem.Directory.GetLastWriteTimeUtc(A<string>.Ignored)).Returns(DateTime.UtcNow);

            var result = bessCleanUpService.CleanUpHistoricFoldersAndFiles();
            result.Should().Be("No folders to delete");

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<EventId>(1) == EventIds.NoFoldersFound.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "No folders to delete based on the cleanup configured date - {historicDate} | DateTime: {DateTime} | Correlation ID: {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenHistoricFoldersDeletionFailed_ThenCleanupJobThrowsExceptionAndLogsError()
        {
            A.CallTo(() => fakeFileSystem.Directory.GetDirectories(A<string>.Ignored)).Returns(fakeFilePath);
            A.CallTo(() => fakeFileSystem.Directory.GetLastWriteTimeUtc(A<string>.Ignored)).Returns(fakeDateTime);
            A.CallTo(() => fakeFileSystem.Directory.Delete(A<string>.Ignored, A<bool>.Ignored)).Throws<Exception>();

            bessCleanUpService.CleanUpHistoricFoldersAndFiles();

            A.CallTo(fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<EventId>(1) == EventIds.FoldersDeletionFailed.ToEventId()
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Could not delete folder: {folderName}. Either could not find the folder or unauthorized access to the folder | DateTime: {DateTime} | Error Message: {ErrorMessage} | _X-Correlation-ID:{CorrelationId}"
            ).MustHaveHappened();
        }
    }
}
