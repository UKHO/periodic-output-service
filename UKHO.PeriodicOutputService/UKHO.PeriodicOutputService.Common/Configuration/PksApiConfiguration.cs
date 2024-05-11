namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public class PksApiConfiguration : IPksApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}