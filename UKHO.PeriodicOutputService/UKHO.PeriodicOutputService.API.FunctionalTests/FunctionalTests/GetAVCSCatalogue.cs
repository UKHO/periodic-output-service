using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    public class GetAVCSCatalogue
    {
        public string userCredentialsBytes;

        private GetUNPResponse getunp { get; set; }
        private TestConfiguration config { get; set; }
        private GetCatalogue getcat { get; set; }

        static FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;    
        
        [OneTimeSetUp]
        public Task Setup()
        {
            config = new TestConfiguration();
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(fleet.userName, fleet.password);
            return Task.CompletedTask;
        }

        [Test]
        public async Task WhenICallTheUNPApiWithNullUserName_ThenABadRequestStatusIsReturned()
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, null, fleet.subscriptionKey);
            Assert.That((int)unpResponse.StatusCode, Is.EqualTo(400), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 400.");
        }

        [Test]
        [TestCase("ABCD", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "WhenICallTheUNPApiWithInvalidSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "WhenICallTheUNPApiWithNullSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        public async Task WhenICallTheUNPApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subkey,string message)
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, subkey);
            Assert.That((int)unpResponse.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 401.");

            string unp_message = await CommonHelper.DeserializeAsyncMessage(unpResponse);
            Assert.That(unp_message, Is.EqualTo(message));
        }

        [Test]
        [TestCase("ER@#$", TestName = "WhenICallTheCatalogueApiWithInValidUnpToken_ThenForbiddentatusIsReturned")]
        [TestCase(null, TestName = "WhenICallTheCatalogueApiWithNullUnpToken_ThenForbiddentatusIsReturned")]
        public async Task WhenICallTheCatalogueApiWithInValidUnpToken_ThenForbiddentatusIsReturned(string unpToken)
        {
            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unpToken, fleet.subscriptionKey);
            Assert.That((int)Catalogue_Response.StatusCode, Is.EqualTo(403), $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 403.");
        }

        [Test]
        [TestCase("ER@#$", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "WhenICallTheCatalogueApiWithInvalidSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "WhenICallTheCatalogueApiWithNullSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        public async Task WhenICallTheCatalogueApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subsKey, string message)
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            Assert.That((int)unpResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {unpResponse.StatusCode}, instead of the expected status 200.");

            string apiResponseToken = await CommonHelper.DeserializeAsyncToken(unpResponse);
            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, apiResponseToken, subsKey);
            Assert.That((int)Catalogue_Response.StatusCode, Is.EqualTo(401), $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 401.");

            string catalogue_message = await CommonHelper.DeserializeAsyncMessage(Catalogue_Response);
            Assert.That(catalogue_message, Is.EqualTo(message));
        }

        [Test]
        public async Task WhenICallTheCatalogueApiWithValidDetails_ThenASuccessResponseIsReturned()
        {
            var unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unp_token = await CommonHelper.DeserializeAsyncToken(unpResponse);

            var Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unp_token, fleet.subscriptionKey);
            Assert.That((int)Catalogue_Response.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {Catalogue_Response.StatusCode}, instead of the expected status 200.");

            var apiResponseDetails_catalogue = await Catalogue_Response.ReadAsStringAsync();
            dynamic apiReadXml = CommonHelper.XmlReadAsynch(apiResponseDetails_catalogue);

            string orgName = apiReadXml.UKHOCatalogueFile.BaseFileMetadata.MD_PointOfContact.ResponsibleParty.organisationName;
            Assert.That(orgName, Is.EqualTo("The United Kingdom Hydrographic Office"), $"Value is not matching, Actual value is : {orgName}");

            string product = apiReadXml.UKHOCatalogueFile.Products.Digital.ENC[0].ShortName;
            Assert.That(product, Is.EqualTo("AR201140"), $"Value is not matching, Actual value is : {product}");
        }
    }
}
