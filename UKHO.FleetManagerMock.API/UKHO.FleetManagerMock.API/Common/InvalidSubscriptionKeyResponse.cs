namespace UKHO.FleetManagerMock.API.Common
{
    public class InvalidSubscriptionKeyResponse
    {
        public int statusCode { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string message { get; set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
