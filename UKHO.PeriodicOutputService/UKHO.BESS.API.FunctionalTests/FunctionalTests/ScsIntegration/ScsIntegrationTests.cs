using System.Net;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.ScsIntegration
{
    public class ScsIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallEssDataEndpoint_ThenSuccessStatusCode200IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.EssDataEndpoint(testConfiguration.scsConfig.BaseUrl);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallEssDataEndpointWithIncorrectURL_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.EssDataEndpoint(testConfiguration.scsConfig.BaseUrl, false);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)400);
        }
    }
}
