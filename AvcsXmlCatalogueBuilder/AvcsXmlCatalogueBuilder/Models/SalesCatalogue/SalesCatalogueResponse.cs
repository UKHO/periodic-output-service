using System.Net;

namespace AvcsXmlCatalogueBuilder.Models.SalesCatalogue
{
    public class SalesCatalogueResponse
    {
        public SalesCatalogueProductResponse ResponseBody { get; set; }
        public HttpStatusCode ResponseCode { get; set; }
        public DateTime? LastModified { get; set; }
    }    
}
