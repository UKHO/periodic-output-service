using System.Text;

namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFulfilmentDataService
    {
        Task<StringBuilder> CreatePosExchangeSet();
    }
}
