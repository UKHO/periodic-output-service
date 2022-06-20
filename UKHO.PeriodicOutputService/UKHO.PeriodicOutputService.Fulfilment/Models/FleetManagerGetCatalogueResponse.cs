using System.Net;

namespace UKHO.PeriodicOutputService.Fulfilment.Models
{
    public class FleetManagerGetCatalogueResponse
    {
        public List<string>? ProductIdentifiers { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
