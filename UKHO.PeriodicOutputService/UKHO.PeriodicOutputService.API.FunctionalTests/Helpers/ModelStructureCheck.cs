using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class ModelStructureCheck
    {
        public static async Task CheckModelStructureForSuccessResponse(this HttpResponseMessage apiResponse)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, $"Response body returns null, instead of expected link {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned batch status URI {apiResponseData.Links.ExchangeSetBatchStatusUri.Href}, Its not valid uri");

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.IsNotNull(apiResponseData.Links.ExchangeSetFileUri.Href, "Response body returns null instead of valid links.");
            Assert.IsTrue(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute), $"Exchange set returned file URI {apiResponseData.Links.ExchangeSetFileUri.Href}, Its not valid uri");

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.IsNotNull(apiResponseData.ExchangeSetUrlExpiryDateTime, $"Response body returns null, Instead of valid datetime {apiResponseData.ExchangeSetUrlExpiryDateTime}.");

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)), "Response body returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductCount >= 0, "Response body returns RequestedProductCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)), "Response body returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.ExchangeSetCellCount >= 0, "Response body returns ExchangeSetCellCount less than zero, instead of expected count should not be less than zero.");

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)), "Response body returns other datatype, instead of expected Int");
            Assert.IsTrue(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0, "Response body returns RequestedProductsAlreadyUpToDateCount less than zero, instead of expected count should not be less than zero.");

        }
    }
}
