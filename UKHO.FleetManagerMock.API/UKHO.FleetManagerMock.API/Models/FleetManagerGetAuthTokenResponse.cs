using System.Net;

namespace UKHO.FleetManagerMock.API.Models
{
    public class FleetManagerGetAuthTokenResponse
    {
        public string? AuthToken { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
