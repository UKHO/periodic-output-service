using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;

namespace UKHO.PeriodicOutputService.Fulfilment
{
    public class PosFulfilmentJob
    {
        private readonly IOptions<FleetManagerB2BApiConfiguration> _fleetManagerB2BApiConfig;

        public PosFulfilmentJob(IOptions<FleetManagerB2BApiConfiguration> fleetManagerB2BApiConfig)
        {
            _fleetManagerB2BApiConfig = fleetManagerB2BApiConfig;
        }

        //This method is queue-triggered for temporary purpose
        public void ProcessWebJob([QueueTrigger("ess-fulfilment-queue")] string message)
        {
            Console.WriteLine(_fleetManagerB2BApiConfig.Value.BaseUrl);
        }
    }
}
