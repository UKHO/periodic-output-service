using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Helpers;

public interface IAzureBlobStorageService
{
    Task<bool> SetConfigQueueMessageModelAndAddToQueueAsync(BessConfig bessConfig, IEnumerable<string> encCellNames, int? fileSize);
}
