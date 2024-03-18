using System.Net;

namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class SalesCatalogueDataResponse
    {
        public List<SalesCatalogueDataProductResponse> ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
