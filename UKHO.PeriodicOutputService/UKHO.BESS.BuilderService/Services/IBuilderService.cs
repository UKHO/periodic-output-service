using UKHO.PeriodicOutputService.Common.Models.Ess.Response;

namespace UKHO.BESS.BuilderService.Services
{
    public interface IBuilderService
    {
        Task<ExchangeSetResponseModel> CreateBespokeExchangeSet();
    }
}
