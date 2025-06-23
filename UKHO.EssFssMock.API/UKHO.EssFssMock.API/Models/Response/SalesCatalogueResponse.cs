using System.Net;

namespace UKHO.EssFssMock.API.Models.Response
{
    public class SalesCatalogueResponse
    {
        public string Id { get; set; }
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }
}
