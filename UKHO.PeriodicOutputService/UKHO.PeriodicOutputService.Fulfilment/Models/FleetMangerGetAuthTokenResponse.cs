using System.Net;

namespace UKHO.PeriodicOutputService.Fulfilment.Models
{
    public class FleetMangerGetAuthTokenResponse
    {
        public string? AuthToken { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
