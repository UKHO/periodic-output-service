namespace UKHO.PeriodicOutputService.Fulfilment.Configuration
{
    public interface IFssApiConfiguration
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string FssClientId { get; set; }
        public string BaseUrl { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.

    }
}
