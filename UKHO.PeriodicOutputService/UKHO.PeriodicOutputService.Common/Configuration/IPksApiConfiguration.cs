namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public interface IPksApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
    }
}