using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;

        public BuilderService(IEssService essService)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
        }

        public async Task<string> CreateBespokeExchangeSet(ConfigQueueMessage message)
        {
            await essService.PostProductIdentifiersData(message.EncCellNames.ToList(), message.ExchangeSetStandard);

            return "Exchange Set Created Successfully";
        }
    }
}
