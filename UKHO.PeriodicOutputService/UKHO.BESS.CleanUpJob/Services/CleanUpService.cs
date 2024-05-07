using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.BESS.CleanUpJob.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob.Services
{
    public class CleanUpService : ICleanUpService
    {
        private readonly IOptions<CleanUpConfiguration> cleanUpConfig;
        private readonly ILogger<CleanUpService> logger;
        private readonly IFileSystem fileSystem;
        private readonly string homeDirectoryPath;

        private const string BESPOKEFOLDERNAME = "BespokeFolderName";
        private const string HOME = "HOME";

        public CleanUpService(IConfiguration configuration, ILogger<CleanUpService> logger, IOptions<CleanUpConfiguration> cleanUpConfig, IFileSystem fileSystem)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cleanUpConfig = cleanUpConfig ?? throw new ArgumentNullException(nameof(cleanUpConfig));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            homeDirectoryPath = configuration == null ? throw new ArgumentNullException(nameof(configuration))
                                : Path.Combine(configuration[HOME]!, configuration[BESPOKEFOLDERNAME]!);
        }

        public string CleanUpHistoricFoldersAndFiles()
        {
            var folderPaths = fileSystem.Directory.GetDirectories(homeDirectoryPath);
            if (!folderPaths.Any())
            {
                logger.LogInformation(EventIds.NoFoldersFound.ToEventId(), "No folders to delete | DateTime: {DateTime} | Correlation ID: {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return "No folders to delete";
            }
            var historicDateTimeInUtc = DateTime.UtcNow.AddDays(-cleanUpConfig.Value.NumberOfDays);
            var historicDate = new DateTime(historicDateTimeInUtc.Year, historicDateTimeInUtc.Month, historicDateTimeInUtc.Day);

            // Filter folders based on last write time
            var foldersToDelete = folderPaths.Where(folderPath => fileSystem.Directory.GetLastWriteTimeUtc(folderPath) <= historicDate).ToList();
            if (!foldersToDelete.Any())
            {
                logger.LogInformation(EventIds.NoFoldersFound.ToEventId(), "No folders to delete based on the cleanup configured date - {historicDate} | DateTime: {DateTime} | Correlation ID: {CorrelationId}", historicDate, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                return "No folders to delete";
            }

            // Deletes all the folders that satisfy the above filter
            string folderName = string.Empty;
            foreach (var folderPath in foldersToDelete)
            {
                try
                {
                    folderName = new DirectoryInfo(folderPath).Name;
                    fileSystem.Directory.Delete(folderPath, true);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.FoldersDeletionFailed.ToEventId(), "Could not delete folder: {folderName}. Either could not find the folder or unauthorized access to the folder | DateTime: {DateTime} | Error Message: {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", folderName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                }
            }

            logger.LogInformation(EventIds.CleanUpSuccessful.ToEventId(), "Successfully cleaned the folder | DateTime: {DateTime} | Correlation ID: {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            return "Successfully cleaned the folder";
        }
    }
}
