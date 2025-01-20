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
            Assert.That(apiResponseData.Links.ExchangeSetBatchStatusUri.Href != null);
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute));

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetFileUri.Href != null);
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute));

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime != null);


            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.RequestedProductCount >= 0);

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.ExchangeSetCellCount >= 0);

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0);
        }

        public static async Task CheckModelStructureForSuccessResponseAio(this HttpResponseMessage apiResponse)
        {
            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check ExchangeSetBatchStatusUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetBatchStatusUri.Href != null);
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute));

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.ExchangeSetFileUri.Href != null);
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute));

            //Check aioExchangeSetFileUri is Not null and it is a valid Uri
            Assert.That(apiResponseData.Links.AioExchangeSetFileUri.Href != null);
            Assert.That(Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute));

            //Check ExchangeSetUrlExpiryDateTime is not null
            Assert.That(apiResponseData.ExchangeSetUrlExpiryDateTime != null);

            //Check data type of RequestedProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.RequestedProductCount >= 0);

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.ExchangeSetCellCount >= 0);

            //Check data type of RequestedAioProductCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedAioProductCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.RequestedAioProductCount == 1);

            //Check data type of AioExchangeSetCellCount and value should not be less than zero
            Assert.That(apiResponseData.AioExchangeSetCellCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.AioExchangeSetCellCount >= 0);

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)));
            Assert.That(apiResponseData.RequestedProductsAlreadyUpToDateCount >= 0);
        }
    }
}
