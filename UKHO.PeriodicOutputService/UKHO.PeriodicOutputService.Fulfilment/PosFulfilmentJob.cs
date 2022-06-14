using System.Text;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
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

        //This method is queue-triggered for temporary purpose
        public async Task ProcessWebJob([QueueTrigger("ess-fulfilment-queue")] string message)
        {
            string result = string.Empty;
            try
            {
                result = await _fulfilmentDataService.CreatePosExchangeSet();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.InnerException);
            }
        }
    }
}
