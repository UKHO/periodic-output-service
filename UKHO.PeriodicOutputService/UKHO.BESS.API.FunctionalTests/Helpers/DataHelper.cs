using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class DataHelper
    {
        public ProductVersionModel? ProductVersionModel { get; set; }

        public ProductVersionModel GetProductVersionData(string productName, int? editionNumber, int? updateNumber)
        {
            ProductVersionModel = new ProductVersionModel()
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };
            return ProductVersionModel;
        }
    }
}
