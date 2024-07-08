using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.BuilderService.Services
{
    public interface IBuilderService
    {
        Task<bool> CreateBespokeExchangeSetAsync(ConfigQueueMessage configQueueMessage);
    }
}
