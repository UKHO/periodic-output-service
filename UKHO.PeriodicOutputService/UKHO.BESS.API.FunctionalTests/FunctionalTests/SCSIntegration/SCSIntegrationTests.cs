using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.ScsIntegration
{
    public class ScsIntegrationTests
    {
        private string? ScsJwtToken { get; set; }
        static readonly TestConfiguration testConfiguration = new();

        [OneTimeSetUp]
        public async Task OneTimeSetupAsync()
        {
            AuthTokenProvider authTokenProvider = new();
            ScsJwtToken = await authTokenProvider.GetScsToken();
        }

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallScsEssDataEndpointWithValidToken_ThenSuccessStatusCode200IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.ScsEssDataEndpoint(testConfiguration.scsConfig.BaseUrl, ScsJwtToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallScsEssDataEndpointWithInValidToken_ThenUnauthorizedStatusCode401IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.ScsEssDataEndpoint(testConfiguration.scsConfig.BaseUrl, testConfiguration.authTokenConfig.FakeToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)401);
        }

        //PBI 140034: BESS CS - Get AVCS catalogue from SCS
        [Test]
        public async Task WhenICallInvalidScsEssDataEndpointWithValidTokenAndWithIncorrectURL_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await ScsEndpointHelper.ScsEssDataEndpoint(testConfiguration.scsConfig.BaseUrl, ScsJwtToken, false);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }
    }
}
