namespace UKHO.PeriodicOutputService.Common.Models.Scs.Response
{
    public class SalesCatalogueProductResponse
    {
        public List<Products> Products { get; set; }
    }

    public class Products
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public List<int?> UpdateNumbers { get; set; }
        public long? FileSize { get; set; }
    }
}
