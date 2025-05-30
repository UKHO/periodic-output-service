using System.Net;

namespace UKHO.PeriodicOutputService.Common.Models.Scs.Response
{
    public class SalesCatalogueResponse
    {
        public SalesCatalogueProductResponse? ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
    }
}
