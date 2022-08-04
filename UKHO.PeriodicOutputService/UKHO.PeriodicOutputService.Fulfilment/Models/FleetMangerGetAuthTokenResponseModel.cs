using System.Net;

namespace UKHO.PeriodicOutputService.Fulfilment.Models
{
    public class FleetMangerGetAuthTokenResponseModel
    {
        public string? AuthToken { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
