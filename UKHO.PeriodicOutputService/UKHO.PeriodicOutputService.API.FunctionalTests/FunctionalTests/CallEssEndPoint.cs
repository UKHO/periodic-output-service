using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("CallEssEndPoint")]
    public class CallEssEndPoint : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            getproductIdentifier = new GetProductIdentifiers();
            EssJwtToken = await AuthTokenProvider.GetEssToken();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GB800001");

            var apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponseAio();

            productIdentifiersAIO.Clear();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInvalidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GC800001");

            var apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponseAio();

            productIdentifiersAIO.Clear();
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            var apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            Assert.That(apiResponse.StatusCode, Is.EqualTo((HttpStatusCode)200));
        }
    }
}
