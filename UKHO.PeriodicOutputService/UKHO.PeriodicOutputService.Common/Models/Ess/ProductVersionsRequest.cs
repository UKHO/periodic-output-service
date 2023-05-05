namespace UKHO.PeriodicOutputService.Common.Models.Ess
{
    public class ProductVersionsRequest
    {
        public List<ProductVersion> ProductVersions { get; set; }
    }

    public class ProductVersion
    {
        public string ProductName { get; set; }
        public int? EditionNumber { get; set; }
        public int? UpdateNumber { get; set; }
    }
}
