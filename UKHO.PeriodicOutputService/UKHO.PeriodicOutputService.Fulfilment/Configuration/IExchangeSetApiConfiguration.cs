namespace UKHO.PeriodicOutputService.Fulfilment.Configuration
{
    public interface IExchangeSetApiConfiguration
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string EssClientId { get; set; }
        public string BaseUrl { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    }
}
