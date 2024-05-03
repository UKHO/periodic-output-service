using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class DataHelper
    {
        public ProductVersionModel? ProductVersionModel { get; set; }
        public ProductKeyServiceModel? ProductKeyServiceModel { get; set; }

        /// <summary>
        /// This method is used to create the product versions endpoint body.
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="editionNumber"></param>
        /// <param name="updateNumber"></param>
        /// <returns></returns>
        public ProductVersionModel GetProductVersionData(string? productName, int? editionNumber, int? updateNumber)
        {
            ProductVersionModel = new ProductVersionModel()
            {
                ProductName = productName,
                EditionNumber = editionNumber,
                UpdateNumber = updateNumber
            };
            return ProductVersionModel;
        }

        /// <summary>
        /// /// This method is used to create the pks data body.
        /// </summary>
        /// <param name="productName"></param>
        /// <param name="edition"></param>
        /// <returns></returns>
        public ProductKeyServiceModel GetProductKeyServiceData(string? productName, int? edition)
        {
            ProductKeyServiceModel = new ProductKeyServiceModel()
            {
                ProductName = productName,
                EditionNumber = edition,
            };
            return ProductKeyServiceModel;
        }
    }
}
