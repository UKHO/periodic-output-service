namespace UKHO.PeriodicOutputService.Fulfilment.Configuration
{
    public interface IFleetManagerB2BApiConfiguration
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string BaseUrl { get; set; }
        public string JwtAuthUnpEndpoint { get; set; }
        public string JwtAuthJwtEndpoint { get; set; }
        public string CatalogueEndpoint { get; set; }
    }
}
