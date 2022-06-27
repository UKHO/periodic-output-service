using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class GetAVCSCatalogue
    {
        public string userCredentialsBytes;

        private GetJwtAuthUnp getunp { get; set; }
        private TestConfiguration config { get; set; }
        private GetCatalogue getcat { get; set; }

        static FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;    
        
        [OneTimeSetUp]
        public async Task Setup()
        {
            config = new TestConfiguration();
            getunp = new GetJwtAuthUnp();
            getcat = new GetCatalogue();

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(fleet.userName , fleet.password);
        }

        [Test]
        public async Task WhenICallTheUNPApiWithNullUserName_ThenABadRequestStatusIsReturned()
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, null, fleet.subscriptionKey);
            Assert.AreEqual(400, (int)unpResponse.StatusCode, $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 400.");
        }

        [Test]
        [TestCase("ABCD", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "Invalid SubscriptionKey for UNP")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "Null SubscriptionKey for UNP")]
        public async Task WhenICallTheUNPApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subkey,string message)
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, subkey);
            Assert.AreEqual(401, (int)unpResponse.StatusCode, $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 401.");

            string unp_message = await CommonHelper.DeserializeAsyncMessage(unpResponse);
            Assert.AreEqual(message, unp_message);
        }

        [Test]
        [TestCase("ER@#$", TestName = "Invalid Unp token for Catalogue")]
        [TestCase(null, TestName = "Null Unp token for Catalogue")]
        public async Task WhenICallTheCatalogueApiWithInValidUnpToken_ThenForbiddentatusIsReturned(string unpToken)
        {
            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unpToken, fleet.subscriptionKey);
            Assert.AreEqual(403, (int)Catalogue_Response.StatusCode, $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 403.");
        }

        [Test]
        [TestCase("ER@#$", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "Invalid Subscription Key for catalogue")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "Null Subscription Key for catalogue")]
        public async Task WhenICallTheCatalogueApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subsKey, string message)
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            Assert.AreEqual(200, (int)unpResponse.StatusCode, $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            string apiResponseToken = await CommonHelper.DeserializeAsyncToken(unpResponse);
            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, apiResponseToken, subsKey);
            Assert.AreEqual(401, (int)Catalogue_Response.StatusCode, $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 401.");

            string catalogue_message = await CommonHelper.DeserializeAsyncMessage(Catalogue_Response);
            Assert.AreEqual(message, catalogue_message);
            
        }

        [Test]
        public async Task WhenICallTheCatalogueApiWithValidDetails_ThenASuccessResponseIsReturned()
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);

            string unp_token = await CommonHelper.DeserializeAsyncToken(unpResponse);
            Thread.Sleep(2000);

            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unp_token, fleet.subscriptionKey);
            Assert.AreEqual(200, (int)Catalogue_Response.StatusCode, $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails_catalogue = await Catalogue_Response.ReadAsStringAsync();
            dynamic apiReadXml = await CommonHelper.XmlReadAsynch(apiResponseDetails_catalogue);

            string ordName = apiReadXml.UKHOCatalogueFile.BaseFileMetadata.MD_PointOfContact.ResponsibleParty.organisationName;
            Assert.AreEqual("The United Kingdom Hydrographic Office", ordName, $"Value is not matching, Actual value is : {ordName}");
            string product = apiReadXml.UKHOCatalogueFile.Products.Digital.ENC[0].ShortName;
            Assert.AreEqual("AR201010", product, $"Value is not matching, Actual value is : {product}");
        }
    }
}
