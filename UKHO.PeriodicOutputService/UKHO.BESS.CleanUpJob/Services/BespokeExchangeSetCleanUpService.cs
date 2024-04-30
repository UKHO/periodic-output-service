using System.IO.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.BESS.CleanUpJob.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.BESS.CleanUpJob.Services
{
    public class BespokeExchangeSetCleanUpService : IBespokeExchangeSetCleanUpService
    {
        private readonly IOptions<CleanUpConfiguration> cleanUpConfig;
        private readonly ILogger<BespokeExchangeSetCleanUpService> logger;
        private readonly IFileSystem fileSystem;
        private readonly string homeDirectoryPath;

        public BespokeExchangeSetCleanUpService(IConfiguration configuration, ILogger<BespokeExchangeSetCleanUpService> logger, IOptions<CleanUpConfiguration> cleanUpConfig, IFileSystem fileSystem)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.cleanUpConfig = cleanUpConfig ?? throw new ArgumentNullException(nameof(cleanUpConfig));
            this.fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            homeDirectoryPath = configuration.Equals(null) ? throw new ArgumentNullException(nameof(configuration))
                                : Path.Combine(configuration["HOME"]!, configuration["BespokeFolderName"]!);
        }
        public async Task DeleteHistoricFoldersAndFiles()
        {
            var directories = fileSystem.Directory.GetDirectories(homeDirectoryPath);
            if (directories.Any())
            {
                var historicDateTimeInUtc = DateTime.UtcNow.AddDays(-cleanUpConfig.Value.NumberOfDays);
                var historicDate = new DateTime(historicDateTimeInUtc.Year, historicDateTimeInUtc.Month, historicDateTimeInUtc.Day);

                directories = directories.Where(directory => fileSystem.Directory.GetLastWriteTimeUtc(directory) <= historicDate).ToArray();
                if (!directories.Any())
                {
                    logger.LogInformation(EventIds.NoArchiveDirectoryFound.ToEventId(), "No directories found older to date - {historicDate} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}", historicDate, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                    return;
                }

                Parallel.ForEach(directories, directory =>
                 {
                     var directoryInfo = new DirectoryInfo(directory);
                     try
                     {
                         fileSystem.Directory.Delete(directory, true);
                     }
                     catch (Exception ex)
                     {
                         logger.LogError(EventIds.DirectoryDeletionFailed.ToEventId(), "Could not delete directory: {directory}. Either could not find the directory or unauthorized access to the directory | DateTime: {DateTime} | Error Message: {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", directoryInfo.Name, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                     }
                 });
            }
            await Task.CompletedTask;
        }
    }
}
