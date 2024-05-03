namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public interface IPksApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string ClientSecret { get; set; }
        public string AccessTokenUrl { get; set; }
        public string TenantId { get; set; }
    }
}
