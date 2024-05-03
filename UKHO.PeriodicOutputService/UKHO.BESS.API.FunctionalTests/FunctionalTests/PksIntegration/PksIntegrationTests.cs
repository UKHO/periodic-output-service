using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.PksIntegration
{
    public class PksIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();
        public DataHelper dataHelper = new();
        private List<ProductKeyServiceModel>? productKeyService { get; set; }

        [Test]
        public async Task WhenICallPksEndpointWithValidProduct_ThenSuccessStatusCode200IsReturned()
        {
            productKeyService = new List<ProductKeyServiceModel> {
            dataHelper.GetProductKeyServiceData(testConfiguration.pksConfig.ProductName,testConfiguration.pksConfig.EditionNumber)
            };

            HttpResponseMessage apiResponse = await PksEndpointHelper.PermitKeysEndpoint(testConfiguration.pksConfig.BaseUrl, productKeyService);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        [TestCase("JP319115",0)]
        [TestCase("AP319115", 8)]
        public async Task WhenICallPksEndpointWithInValidProduct_ThenSuccessStatusCode404IsReturned(string productName, int editionNumber) //Need to change to 400
        {
            productKeyService = new List<ProductKeyServiceModel> {
            dataHelper.GetProductKeyServiceData(productName,editionNumber)
            };

            HttpResponseMessage apiResponse = await PksEndpointHelper.PermitKeysEndpoint(testConfiguration.pksConfig.BaseUrl, productKeyService);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)404); //Need to change to 400
        }
    }
}
