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
        /// <param name="productName">Sets the product provided into the model</param>
        /// <param name="editionNumber">Sets the Edition for the provided product into the model</param>
        /// <param name="updateNumber">Sets the Update for the provided product into the model</param>
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
        /// <param name="productName">Sets the product provided into the model</param>
        /// <param name="edition">Sets the Edition for the provided product into the model</param>
        /// <returns></returns>
        public ProductKeyServiceModel GetProductKeyServiceData(string? productName, string? edition)
        {
            ProductKeyServiceModel = new ProductKeyServiceModel()
            {
                productName = productName,
                edition = edition,
            };
            return ProductKeyServiceModel;
        }
    }
}
