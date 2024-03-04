using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.MockEndPoint
{
    public class UploadBessConfigFileTests
    {
        static readonly TestConfiguration testConfiguration = new();

        [Test]
        public async Task WhenICallUploadConfigApiWithValidConfigFile_ThenCreatedStatusCode201IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)201);
        }

        [Test]
        public async Task WhenICallUploadConfigApiWithInvalidConfigFile_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.InvalidConfigPath);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }
    }
}
