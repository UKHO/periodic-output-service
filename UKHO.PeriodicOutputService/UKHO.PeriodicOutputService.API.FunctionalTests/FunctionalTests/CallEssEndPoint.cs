using System.Net;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("CallEssEndPoint")]
    public class CallEssEndPoint : ObjectStorage
    {
        [OneTimeSetUp]
        public async Task Setup()
        {
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();
            getproductIdentifier = new GetProductIdentifiers();

            userCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(fleet.userName, fleet.password);
            AuthTokenProvider authTokenProvider = new();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidProductIdentifier);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unpToken = await unpResponse.DeserializeAsyncToken();
            HttpResponseMessage httpResponse = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unpToken, fleet.subscriptionKey);
            productIdentifiers = await getcat.GetProductList(httpResponse);

            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidProductIdentifiers_ThenANonZeroRequestedProductCountAndExchangeSetCellCountIsReturned()
        {
            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            Assert.Equals(apiResponse.StatusCode,(HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidRequestedProductCountAndLessExchangeSetCellCountIsReturned()
        {
            productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            ExchangeSetResponseModel apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.Count() > 0);
            Assert.Equals(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault(p => p.ProductName.Equals("ABCDEFGH")).Reason, "invalidProduct");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenALargeMediaStructureIsCreated()
        {
            HttpResponseMessage essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            Assert.Equals(essApiResponse.StatusCode, (HttpStatusCode)200);

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForLargeMedia(posDetails.ZipFilesBatchId, FssJwtToken);
            Assert.Equals(DownloadedFolderPath.Count, 2);
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GB800001");

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponseAio();

            productIdentifiersAIO.Clear();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInvalidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GC800001");

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);

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
            HttpResponseMessage apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            Assert.Equals(apiResponse.StatusCode, (HttpStatusCode)200);
        }
    }
}
