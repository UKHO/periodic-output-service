using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Fulfilment.Configuration
{
    [ExcludeFromCodeCoverage]
    public class FleetManagerB2BApiConfiguration : IFleetManagerB2BApiConfiguration
    {
        #pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseUrl { get; set; }
        public string SubscriptionKey { get; set; }
        #pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
