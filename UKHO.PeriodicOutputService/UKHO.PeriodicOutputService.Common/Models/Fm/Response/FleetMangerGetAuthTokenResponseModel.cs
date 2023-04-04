using System.Net;

namespace UKHO.PeriodicOutputService.Common.Models.Fm.Response
{
    public class FleetMangerGetAuthTokenResponseModel
    {
        public string? AuthToken { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
