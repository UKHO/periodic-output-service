using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Helpers;

namespace UKHO.BESS.API.FunctionalTests.FunctionalTests
{
    public class UploadBessConfigFileTests
    {
        static readonly TestConfiguration testConfiguration = new();

        [Test]
        public async Task WhenICallUploadConfigApiWithValidConfigFile_ThenCreatedStatusCodeIsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, @"./TestData/configFile.json");
            apiResponse.StatusCode.Should().Be((HttpStatusCode)201);
        }

        [Test]
        public async Task WhenICallUploadConfigApiWithInvalidConfigFile_ThenCreatedStatusCodeIsReturned()
        {
            HttpResponseMessage apiResponse = await BessUploadFileHelper.UploadConfigFile(testConfiguration.bessConfig.BaseUrl, @"./TestData/InvalidConfigFile.json");
            apiResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }
    }
}
