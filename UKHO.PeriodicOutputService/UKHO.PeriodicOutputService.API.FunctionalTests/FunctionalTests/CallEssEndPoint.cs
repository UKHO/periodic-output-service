using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [TestFixture, Order(1)]
    public class CallEssEndPoint
    {
        public string userCredentialsBytes;

        private GetUNPResponse getunp { get; set; }
        private GetCatalogue getcat { get; set; }
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private GetProductIdentifiers getproductIdentifier { get; set; }

        private static readonly ESSApiConfiguration ESSAuth = new TestConfiguration().EssConfig;
        private static readonly FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private List<string> productIdentifiers = new();
        private HttpResponseMessage unpResponse;
        private List<string> DownloadedFolderPath;


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

            unpResponse = await getunp.GetJwtAuthUnpToken(fleet.baseUrl, userCredentialsBytes, fleet.subscriptionKey);
            string unpToken = await unpResponse.DeserializeAsyncToken();
            HttpResponseMessage httpResponse = await getcat.GetCatalogueEndpoint(fleet.baseUrl, unpToken, fleet.subscriptionKey);
            productIdentifiers = await getcat.GetProductList(httpResponse);

            unpResponse.StatusCode.Should().Be((HttpStatusCode)200, "Catalogue endpoint");

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            await CommonHelper.RunWebJob();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidProductIdentifiers_ThenANonZeroRequestedProductCountAndExchangeSetCellCountIsReturned()
        {
            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidRequestedProductCountAndLessExchangeSetCellCountIsReturned()
        {
            productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            ExchangeSetResponseModel apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            apiResponseData.RequestedProductsNotInExchangeSet.Should().NotBeEmpty();
            apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault(p => p.ProductName.Equals("ABCDEFGH")).Reason.Should().Be("invalidProduct");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenALargeMediaStructureIsCreated()
        {
            HttpResponseMessage essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiers, EssJwtToken);
            essApiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            DownloadedFolderPath = await FileContentHelper.CreateExchangeSetFileForLargeMedia(posDetails.ZipFilesBatchId, FssJwtToken);
            DownloadedFolderPath.Count.Should().Be(2);
        }

        [OneTimeTearDown]
        public void GlobalTearDown()
        {
            //cleaning up the downloaded files from temp folder
            FileContentHelper.DeleteTempDirectory(posDetails.TempFolderName);

            //cleaning up the stub home directory
            HttpResponseMessage apiResponse = MockHelper.Cleanup(posWebJob.MockApiBaseUrl);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
    }
}
