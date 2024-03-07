using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests.MockEndPoint
{
    public class UploadBessConfigFileTests
    {
        static readonly TestConfiguration testConfiguration = new();

        //PBI 142682: BESS CS : Create a mock endpoint to upload BESS configuration files to azure storage
        [Test]
        public async Task WhenICallUploadConfigApiWithValidConfigFile_ThenCreatedStatusCode201IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.ValidConfigPath, testConfiguration.sharedKeyConfig.Key);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)201);
        }

        //PBI 142682: BESS CS : Create a mock endpoint to upload BESS configuration files to azure storage
        [Test]
        public async Task WhenICallUploadConfigApiWithInvalidConfigFile_ThenBadRequestStatusCode400IsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, testConfiguration.bessConfig.InvalidConfigPath, testConfiguration.sharedKeyConfig.Key);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }
    }
}
