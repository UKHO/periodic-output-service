using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class CallEssEndPoint
    {
        public string userCredentialsBytes;

        private GetUNPResponse getunp { get; set; }
        private TestConfiguration config { get; set; }
        private GetCatalogue getcat { get; set; }
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private GetProductIdentifiers getproductIdentifier { get; set; }

        private static readonly EssAuthorizationConfiguration ESSAuth = new TestConfiguration().EssAuthorizationConfig;
        private static readonly FunctionalTestFSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        private static readonly FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;
        private List<string> productIdentifiers = new();
        private HttpResponseMessage unpResponse;
        private List<string> DownloadedFolderPath;
        
        [OneTimeSetUp]
        public async Task Setup()
        {
            config = new TestConfiguration();
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();
            getproductIdentifier = new GetProductIdentifiers();

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(fleet.userName, fleet.password);
            AuthTokenProvider authTokenProvider = new();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();

            unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unp_token = await CommonHelper.DeserializeAsyncToken(unpResponse);

            HttpResponseMessage httpResponse = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unp_token, fleet.subscriptionKey);

            productIdentifiers = await getcat.GetProductList(httpResponse);
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidProductIdentifiers_ThenANonZeroRequestedProductCountAndExchangeSetCellCountIsReturned()
        {
            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidRequestedProductCountAndLessExchangeSetCellCountIsReturned()
        {
            productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            ExchangeSetResponseModel apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault(p => p.ProductName.Equals("ABCDEFGH")).Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenALargeMediaStructureIsCreated()
        {
            HttpResponseMessage essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)essApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForLargeMedia(essApiResponse, FssJwtToken);
            Assert.That((int)DownloadedFolderPath.Count, Is.EqualTo(2), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //Clean up downloaded files/folders
            foreach (string FolderName in DownloadedFolderPath)
            {
                FileContentHelper.DeleteDirectory(FolderName);
            }
        }
    }
}
