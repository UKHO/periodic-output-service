using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.EssIntegration
{
    public class EssIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();
        public DataHelper dataHelper { get; set; }
        protected List<string> productIdentifiers = new() { "GB301910" };
        private List<ProductVersionModel>? productVersionData { get; set; }

        [OneTimeSetUp]
        public async Task SetupAsync()
        {
            dataHelper = new DataHelper();
            productVersionData = new List<ProductVersionModel>();
            HttpResponseMessage apiResponse = Extensions.ConfigureFM(testConfiguration.bessConfig.BaseUrl, "Identifiers");
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
     
        //PBI 140038
            [Test]
        public async Task ProductIdentifierEndPointOKRequest()
        {
            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductIdentifiersEndpoint(testConfiguration.authTokenConfig.BaseUrl, productIdentifiers, "s63");
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        public async Task ProductIdentifierEndPointBADRequest()
        {
            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductIdentifiersEndpoint(testConfiguration.authTokenConfig.BaseUrl, productIdentifiers, "s57", false);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }

        [Test]
        public async Task ProductVersionEndPointOKRequest()
        {
            productVersionData = new List<ProductVersionModel> {
            dataHelper.GetProductVersionData("GB301910", 1, 0)
            };

            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductVersionsEndpoint(testConfiguration.authTokenConfig.BaseUrl, productVersionData, "s63", true);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        [Test]
        public async Task ProductVersionEndPointBADRequest()
        {
            productVersionData = new List<ProductVersionModel> {
            dataHelper.GetProductVersionData("GB301910", 1, 0)
            };

            HttpResponseMessage apiResponse = await EssEndpointHelper.ProductVersionsEndpoint(testConfiguration.authTokenConfig.BaseUrl, productVersionData, "s63", false);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }

    }
}
