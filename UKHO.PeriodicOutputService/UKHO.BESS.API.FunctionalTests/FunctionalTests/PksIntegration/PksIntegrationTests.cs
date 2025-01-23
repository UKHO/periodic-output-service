using System.Net;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.PksIntegration
{
    public class PksIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();
        public DataHelper dataHelper = new();
        private List<ProductKeyServiceModel>? productKeyService = [];

        [Test]
        public async Task WhenICallPksEndpointWithValidProduct_ThenSuccessStatusCode200IsReturned()
        {
            for (int i = 0; i < testConfiguration.bessConfig.ProductsName!.Count; i++)
            {
                productKeyService!.Add(dataHelper.GetProductKeyServiceData(testConfiguration.bessConfig.ProductsName[i], testConfiguration.bessConfig.EditionNumber![i]));
            }

            HttpResponseMessage apiResponse = await PksEndpointHelper.PermitKeysEndpoint(testConfiguration.pksConfig.BaseUrl, productKeyService!);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
        }

        [Test]
        [TestCase("GB301910", "5")]
        public async Task WhenICallPksEndpointWithInvalidProduct_ThenBadRequestStatusCode400IsReturned(string productName, string editionNumber)
        {
            productKeyService = [
            dataHelper.GetProductKeyServiceData(productName, editionNumber)
            ];

            HttpResponseMessage apiResponse = await PksEndpointHelper.PermitKeysEndpoint(testConfiguration.pksConfig.BaseUrl, productKeyService);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)400));
        }

        [TearDown]
        public void TearDown()
        {
            productKeyService!.Clear();
        }
    }
}
