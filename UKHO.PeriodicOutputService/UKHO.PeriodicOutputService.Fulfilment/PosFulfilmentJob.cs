using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    public class PosFulfilmentJob
    {
        private readonly IFulfilmentDataService _fulfilmentDataService;

        public PosFulfilmentJob(IFulfilmentDataService fulfilmentDataService)
        {
            _fulfilmentDataService = fulfilmentDataService;
        }

        public async Task ProcessFulfilmentJob()
        {
            try
            {
                string result = await _fulfilmentDataService.CreatePosExchangeSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }
    }
}
