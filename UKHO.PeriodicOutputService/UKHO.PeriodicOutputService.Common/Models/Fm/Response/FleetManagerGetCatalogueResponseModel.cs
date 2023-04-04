using System.Net;

namespace UKHO.PeriodicOutputService.Common.Models.Fm.Response
{
    public class FleetManagerGetCatalogueResponseModel
    {
        public List<string> ProductIdentifiers { get; set; }
        public HttpStatusCode StatusCode { get; set; }
    }
}
