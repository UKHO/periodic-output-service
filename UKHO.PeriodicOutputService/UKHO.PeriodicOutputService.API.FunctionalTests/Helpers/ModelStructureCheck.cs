using FluentAssertions;
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
            apiResponseData.Links.ExchangeSetBatchStatusUri.Href.Should().NotBeNull();
            Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetBatchStatusUri.Href, UriKind.RelativeOrAbsolute).Should().BeTrue();

            //Check ExchangeSetFileUri is Not null and it is a valid Uri
            apiResponseData.Links.ExchangeSetFileUri.Href.Should().NotBeNull();
            Uri.IsWellFormedUriString(apiResponseData.Links.ExchangeSetFileUri.Href, UriKind.RelativeOrAbsolute).Should().BeTrue();

            //Check ExchangeSetUrlExpiryDateTime is not null
            apiResponseData.ExchangeSetUrlExpiryDateTime.Should().NotBeNull();


            //Check data type of RequestedProductCount and value should not be less than zero
            apiResponseData.RequestedProductCount.GetType().Equals(typeof(int)).Should().BeTrue();
            apiResponseData.RequestedProductCount.Should().BeGreaterThanOrEqualTo(0);

            //Check data type of ExchangeSetCellCount and value should not be less than zero
            apiResponseData.ExchangeSetCellCount.GetType().Equals(typeof(int)).Should().BeTrue();
            apiResponseData.ExchangeSetCellCount.Should().BeGreaterThanOrEqualTo(0);

            //Check data type of RequestedProductsAlreadyUpToDateCount and value should not be less than zero
            apiResponseData.RequestedProductsAlreadyUpToDateCount.GetType().Equals(typeof(int)).Should().BeTrue();
            apiResponseData.RequestedProductsAlreadyUpToDateCount.Should().BeGreaterThanOrEqualTo(0);
        }
    }
}
