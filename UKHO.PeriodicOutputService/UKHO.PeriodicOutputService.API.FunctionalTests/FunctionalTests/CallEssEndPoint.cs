using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Enums;
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
        private GetBatchStatus getBatchStatus { get; set; }

        static EssAuthorizationConfiguration ESSAuth = new TestConfiguration().EssAuthorizationConfig;
        static FileShareService FSSAuth = new TestConfiguration().FssConfig;
        static FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;

        List<string> productIdentifiers = new();
        HttpResponseMessage unpResponse;

        [OneTimeSetUp]
        public async Task Setup()
        {
            config = new TestConfiguration();
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();
            getproductIdentifier = new GetProductIdentifiers();
            getBatchStatus = new GetBatchStatus();

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(fleet.userName, fleet.password);
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
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
            var apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidRequestedProductCountAndLessExchangeSetCellCountIsReturned()
        {
            productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            var apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            var apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault(p => p.ProductName.Equals("ABCDEFGH")).Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenABatchStatusIsReturned()
        {
            var essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)essApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await essApiResponse.CheckModelStructureForSuccessResponse();
            var essApiResponseData = await essApiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            var fssBatchStatus = await getBatchStatus
                .CheckIfBatchCommitted(FSSAuth.BaseUrl,
                                       essApiResponseData.Links.ExchangeSetBatchStatusUri.Href,
                                       FssJwtToken,
                                       FSSAuth.BatchStatusPollingCutoffTime,
                                       FSSAuth.BatchStatusPollingDelayTime);

            Assert.That(fssBatchStatus, Is.AnyOf(FssBatchStatus.Incomplete, FssBatchStatus.Committed),"The response data is not empty");
        }
    }
}
