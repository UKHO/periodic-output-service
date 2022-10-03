using System.Collections.Generic;

namespace AvcsXmlCatalogueBuilder.Models.SalesCatalogue
{
    public class SalesCatalogueProductResponse
    {
        public List<Products> Products { get; set; }

        public ProductCounts ProductCounts { get; set; }

    }
}
