using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IAzureBlobStorageService
    {
        Task<bool> SetConfigQueueMessageModelAndAddToQueue(BessConfig bessConfig, IEnumerable<string> encCellNames, int? fileSize);
    }
}
