using System.Net;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.EssIntegration
{
    public class EssIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();
        public DataHelper dataHelper = new();
        protected List<string> productIdentifiers = new() { testConfiguration.authTokenConfig.ProductName! };
        private List<ProductVersionModel>? productVersionData { get; set; }

        [OneTimeSetUp]
        public void SetupAsync()
        {
            productVersionData = new List<ProductVersionModel>();
            HttpResponseMessage apiResponse = Extensions.ConfigureFt(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.Identifiers);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }

        //Product Backlog Item 140038: BESS BS - AD auth for ESS and FSS API
        [Test]
        public async Task WhenICallEssProductIdentifierEndpoint_ThenSuccessStatusCode200IsReturned()
        {
            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductIdentifiersEndpoint(testConfiguration.authTokenConfig.BaseUrl, productIdentifiers, testConfiguration.bessConfig.s57ExchangeSetStandard);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }

        //Product Backlog Item 140038: BESS BS - AD auth for ESS and FSS API
        [Test]
        public async Task WhenICallEssProductIdentifierEndpointWithIncorrectURL_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductIdentifiersEndpoint(testConfiguration.authTokenConfig.BaseUrl, productIdentifiers, testConfiguration.bessConfig.s63ExchangeSetStandard, false);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)400);
        }

        //Product Backlog Item 140038: BESS BS - AD auth for ESS and FSS API
        [Test]
        public async Task WhenICallEssProductVersionEndpoint_ThenSuccessStatusCode200IsReturned()
        {
            productVersionData = new List<ProductVersionModel> {
            dataHelper.GetProductVersionData(testConfiguration.authTokenConfig.ProductName, testConfiguration.authTokenConfig.EditionNumber, testConfiguration.authTokenConfig.UpdateNumber)
            };

            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductVersionsEndpoint(testConfiguration.authTokenConfig.BaseUrl, productVersionData, testConfiguration.bessConfig.s57ExchangeSetStandard);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }

        //Product Backlog Item 140038: BESS BS - AD auth for ESS and FSS API
        [Test]
        public async Task WhenICallEssProductVersionEndpointWithIncorrectURL_ThenBadRequestStatusCode400IsReturned()
        {
            productVersionData = new List<ProductVersionModel> {
            dataHelper.GetProductVersionData(testConfiguration.authTokenConfig.ProductName, testConfiguration.authTokenConfig.EditionNumber, testConfiguration.authTokenConfig.UpdateNumber)
            };

            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductVersionsEndpoint(testConfiguration.authTokenConfig.BaseUrl, productVersionData, testConfiguration.bessConfig.s63ExchangeSetStandard, false);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)400);
        }

    }
}
