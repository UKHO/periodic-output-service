using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
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
        private GetProductIdentifiers getproductIdentifier { get; set; }

        static EssAuthorizationConfiguration ESSAuth = new TestConfiguration().EssAuthorizationConfig;
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

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(fleet.userName, fleet.password);
            AuthTokenProvider authTokenProvider = new AuthTokenProvider();
            EssJwtToken = await authTokenProvider.GetEssToken();

            unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unp_token = await CommonHelper.DeserializeAsyncToken(unpResponse);

            HttpResponseMessage httpResponse = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unp_token, fleet.subscriptionKey);

            productIdentifiers = await getcat.GetProductList(httpResponse);
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidProductIdentifiers_ThenValidrequestedProductCountAndexchangeSetCellCountIsReturned()
        {
            var productIdenResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)productIdenResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 401.");

            dynamic prodId_message = await CommonHelper.DeserializeAsyncResponse(productIdenResponse);
            Assert.That(prodId_message._links.exchangeSetBatchStatusUri.href, Is.Not.Null,"Exchange Set Batch Status Uri is null");
            Assert.That(prodId_message._links.exchangeSetBatchDetailsUri.href, Is.Not.Null,"Exchange Set Batch Deatils Uri is null");
            Assert.That(prodId_message._links.exchangeSetFileUri.href, Is.Not.Null,"Exchange Set File Uri is null");
            Assert.That((int)prodId_message.requestedProductCount, Is.EqualTo(3155), $"Incorrect Requested Product Count is returned {prodId_message.requestedProductCount}, instead of the expected status 3155.");
            Assert.That((int)prodId_message.exchangeSetCellCount, Is.EqualTo(3155), $"Incorrect Exchange Set Cell Count is returned {prodId_message.exchangeSetCellCount}, instead of the expected status 3155.");
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidrequestedProductCountAndlessexchangeSetCellCountIsReturned()
        {
            productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            var productIdenResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.EssApiUrl, productIdentifiers, EssJwtToken);
            Assert.That((int)productIdenResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 401.");

            dynamic prodId_message = await CommonHelper.DeserializeAsyncResponse(productIdenResponse);
            string requestedProductCount = prodId_message.requestedProductCount;
            Assert.That((int)prodId_message.requestedProductCount, Is.EqualTo(3156), $"Incorrect Requested Product Count is returned {prodId_message.requestedProductCount}, instead of the expected status 3156.");
            Assert.That((int)prodId_message.exchangeSetCellCount, Is.EqualTo(3155), $"Incorrect Exchange Set Cell Count is returned {prodId_message.exchangeSetCellCount}, instead of the expected status 3155.");

            string InvalidProductName = prodId_message.requestedProductsNotInExchangeSet[0].productName;
            string InvalidProductReason = prodId_message.requestedProductsNotInExchangeSet[0].reason;
            Assert.That(InvalidProductName, Is.EqualTo("ABCDEFGH"), $"Incorrect Product Name is returned {InvalidProductName}, instead of the expected Product Name ABCDEFGH.");
            Assert.That(InvalidProductReason, Is.EqualTo("invalidProduct"), $"Incorrect reason is returned {InvalidProductReason}, instead of the expected reason as invalidProduct.");

            productIdentifiers.Remove("ABCDEFGH"); //Removing invalid product identifier from list so that other test won't get affected.
        }
    }
}
