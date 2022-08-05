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
        private GetBatchElements getBatchElements { get; set; }

        private GetDownloadsAndProcessFile _downloadsAndProcessFile { get; set; }

        private static readonly EssAuthorizationConfiguration s_ESSAuth = new TestConfiguration().EssAuthorizationConfig;
        private static readonly FunctionalTestFSSApiConfiguration s_FSSAuth = new TestConfiguration().FssConfig;
        private static readonly FleetManagerB2BApiConfiguration s_fleet = new TestConfiguration().fleetManagerB2BConfig;
        private List<string> _productIdentifiers = new();
        private HttpResponseMessage _unpResponse;

        [OneTimeSetUp]
        public async Task Setup()
        {
            config = new TestConfiguration();
            getunp = new GetUNPResponse();
            getcat = new GetCatalogue();
            getproductIdentifier = new GetProductIdentifiers();
            getBatchElements = new();
            _downloadsAndProcessFile = new();

            userCredentialsBytes = CommonHelper.getbase64encodedcredentials(s_fleet.userName, s_fleet.password);
            AuthTokenProvider authTokenProvider = new();
            EssJwtToken = await authTokenProvider.GetEssToken();
            FssJwtToken = await authTokenProvider.GetFssToken();

            _unpResponse = await getunp.GetJwtAuthUnpToken(s_fleet.baseUrl, userCredentialsBytes, s_fleet.subscriptionKey);
            string unp_token = await CommonHelper.DeserializeAsyncToken(_unpResponse);

            HttpResponseMessage httpResponse = await getcat.GetCatalogueEndpoint(s_fleet.baseUrl, unp_token, s_fleet.subscriptionKey);

            _productIdentifiers = await getcat.GetProductList(httpResponse);
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidProductIdentifiers_ThenANonZeroRequestedProductCountAndExchangeSetCellCountIsReturned()
        {
            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(s_ESSAuth.EssApiUrl, _productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {_unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInValidProductIdentifiers_ThenValidRequestedProductCountAndLessExchangeSetCellCountIsReturned()
        {
            _productIdentifiers.Add("ABCDEFGH"); //Adding invalid product identifier in the list

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(s_ESSAuth.EssApiUrl, _productIdentifiers, EssJwtToken);
            Assert.That((int)apiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {_unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponse();

            ExchangeSetResponseModel apiResponseData = await apiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            //Check RequestedProductsNotInExchangeSet is not empty
            Assert.IsNotEmpty(apiResponseData.RequestedProductsNotInExchangeSet, "Response body returns Empty for RequestedProductsNotInExchangeSet, instead of Not Empty");
            Assert.That(apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault(p => p.ProductName.Equals("ABCDEFGH")).Reason, Is.EqualTo("invalidProduct"), $"Exchange set returned Reason {apiResponseData.RequestedProductsNotInExchangeSet.FirstOrDefault().Reason}, instead of expected Reason 'invalidProduct'");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenABatchStatusIsReturned()
        {
            HttpResponseMessage essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(s_ESSAuth.EssApiUrl, _productIdentifiers, EssJwtToken);
            Assert.That((int)essApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {_unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await essApiResponse.CheckModelStructureForSuccessResponse();
            ExchangeSetResponseModel essApiResponseData = await essApiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            FssBatchStatus fssBatchStatus = await getBatchElements
                .CheckIfBatchCommitted(s_FSSAuth.BaseUrl,
                                       essApiResponseData.Links.ExchangeSetBatchStatusUri.Href,
                                       FssJwtToken,
                                       s_FSSAuth.BatchStatusPollingCutoffTime,
                                       s_FSSAuth.BatchStatusPollingDelayTime);

            Assert.That(fssBatchStatus, Is.AnyOf(FssBatchStatus.Incomplete, FssBatchStatus.Committed), "The response data is not empty");
        }

        [Test]
        public async Task WhenICallTheFSSApiWithValidBatchId_ThenDownloadExchangeSet()
        {
            HttpResponseMessage essApiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(s_ESSAuth.EssApiUrl, _productIdentifiers, EssJwtToken);
            Assert.That((int)essApiResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {_unpResponse.StatusCode}, instead of the expected status 200.");

            //verify model structure
            await essApiResponse.CheckModelStructureForSuccessResponse();
            ExchangeSetResponseModel essApiResponseData = await essApiResponse.ReadAsTypeAsync<ExchangeSetResponseModel>();

            string batchId = CommonHelper.ExtractBatchId(essApiResponseData.Links.ExchangeSetBatchStatusUri.Href);

            FssBatchStatus fssBatchStatus = await getBatchElements
                .CheckIfBatchCommitted(s_FSSAuth.BaseUrl,
                                       batchId,
                                       FssJwtToken,
                                       s_FSSAuth.BatchStatusPollingCutoffTime,
                                       s_FSSAuth.BatchStatusPollingDelayTime);

            Assert.That(fssBatchStatus, Is.EqualTo(FssBatchStatus.Committed), "The Fss batch status is not committed");

            List<FssBatchFile> fileDetails = await getBatchElements.GetBatchFiles(s_FSSAuth.BaseUrl, batchId, FssJwtToken);

            string downloadPath = Path.Combine(@"D:\HOME", batchId);
            Directory.CreateDirectory(downloadPath);

            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                Stream stream = _downloadsAndProcessFile.DownloadFile(s_FSSAuth.BaseUrl, file.FileLink, FssJwtToken).Result;
                byte[] bytes = _downloadsAndProcessFile.ConvertStreamToByteArray(stream);
                _downloadsAndProcessFile.CreateFileCopy(filePath, new MemoryStream(bytes));

                Assert.That(File.Exists(filePath), Is.True, "File Downloaded failed");
            });

        }
    }
}
