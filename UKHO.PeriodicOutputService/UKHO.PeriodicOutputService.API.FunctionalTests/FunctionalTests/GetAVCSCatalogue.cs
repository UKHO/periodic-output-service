using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("GetAVCSCatalogue")]
    public class GetAVCSCatalogue : ObjectStorage
    {
        [OneTimeSetUp]
        public Task Setup()
        {
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            userCredentialsBytes = CommonHelper.GetBase64EncodedCredentials(fleet.userName, fleet.password);
            return Task.CompletedTask;
        }

        [Test]
        public async Task WhenICallTheUNPApiWithNullUserName_ThenABadRequestStatusIsReturned()
        {
            HttpResponseMessage unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, null, fleet.subscriptionKey);
            unpResponse.StatusCode.Should().Be((HttpStatusCode)400);
        }

        [Test]
        [TestCase("ABCD", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "WhenICallTheUNPApiWithInvalidSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "WhenICallTheUNPApiWithNullSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        public async Task WhenICallTheUNPApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subkey, string message)
        {
            HttpResponseMessage unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, subkey);
            unpResponse.StatusCode.Should().Be((HttpStatusCode)401);

            string unp_message = await CommonHelper.DeserializeAsyncMessage(unpResponse);
            unp_message.Should().Be(message);
        }

        [Test]
        [TestCase("ER@#$", TestName = "WhenICallTheCatalogueApiWithInValidUnpToken_ThenForbiddentatusIsReturned")]
        [TestCase(null, TestName = "WhenICallTheCatalogueApiWithNullUnpToken_ThenForbiddentatusIsReturned")]
        public async Task WhenICallTheCatalogueApiWithInValidUnpToken_ThenForbiddentatusIsReturned(string unpToken)
        {
            HttpResponseMessage Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unpToken, fleet.subscriptionKey);
            Catalogue_Response.StatusCode.Should().Be((HttpStatusCode)403);
        }

        [Test]
        [TestCase("ER@#$", "Access denied due to invalid subscription key. Make sure to provide a valid key for an active subscription.", TestName = "WhenICallTheCatalogueApiWithInvalidSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        [TestCase(null, "Access denied due to missing subscription key. Make sure to include subscription key when making requests to an API.", TestName = "WhenICallTheCatalogueApiWithNullSubscription_ThenAnUnauthorizedRequestStatusIsReturned")]
        public async Task WhenICallTheCatalogueApiWithInValidSubscriptionKey_ThenUnauthorizedStatusIsReturned(string subsKey, string message)
        {
            HttpResponseMessage unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            unpResponse.StatusCode.Should().Be((HttpStatusCode)200);

            string apiResponseToken = await CommonHelper.DeserializeAsyncToken(unpResponse);
            HttpResponseMessage Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, apiResponseToken, subsKey);
            Catalogue_Response.StatusCode.Should().Be((HttpStatusCode)401);

            string catalogue_message = await CommonHelper.DeserializeAsyncMessage(Catalogue_Response);
            catalogue_message.Should().Be(message);
        }

        [Test]
        public async Task WhenICallTheCatalogueApiWithValidDetails_ThenASuccessResponseIsReturned()
        {
            HttpResponseMessage unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unp_token = await CommonHelper.DeserializeAsyncToken(unpResponse);

            HttpResponseMessage Catalogue_Response = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unp_token, fleet.subscriptionKey);
            Catalogue_Response.StatusCode.Should().Be((HttpStatusCode)200);

            string apiResponseDetails_catalogue = await Catalogue_Response.ReadAsStringAsync();
            dynamic apiReadXml = CommonHelper.XmlReadAsynch(apiResponseDetails_catalogue);

            string orgName = apiReadXml.UKHOCatalogueFile.BaseFileMetadata.MD_PointOfContact.ResponsibleParty.organisationName;
            orgName.Should().Be("The United Kingdom Hydrographic Office");

            string product = apiReadXml.UKHOCatalogueFile.Products.Digital.ENC[0].ShortName;
            product.Should().Be("FR570300");
        }
    }
}
