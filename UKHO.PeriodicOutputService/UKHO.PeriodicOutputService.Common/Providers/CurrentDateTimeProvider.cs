namespace UKHO.PeriodicOutputService.Common.Providers
{
    public class CurrentDateTimeProvider : ICurrentDateTimeProvider
    {
        public DateTime CurrentDateTime => DateTime.UtcNow;
    }
}
