using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.ScsIntegration
{
    public class ScsIntegrationTests
    {
        static readonly TestConfiguration testConfiguration = new();

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallEssDataEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.EssDataEndpoint(testConfiguration.scsConfig.MockBaseUrl);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallEssDataEndpointWithValidTokenWithIncorrectURL_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.EssDataEndpoint(testConfiguration.scsConfig.MockBaseUrl, false);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }
    }
}
