using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
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

        public async Task<ExchangeSetResponseModel> CreateBespokeExchangeSet()
        {
            //Temporary code
            var productIdentifiers = new List<string>() { };
            string exchangeSetStandard = "";

            return await essService.PostProductIdentifiersData(productIdentifiers, exchangeSetStandard);
        }
    }
}
