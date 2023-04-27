using System.Net;
using FluentAssertions;
using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.FunctionalTests
{
    [Category("CallEssEndPoint")]
    public class CallEssEndPoint
    {
        public string userCredentialsBytes;

        private GetUNPResponse getunp { get; set; }
        private GetCatalogue getcat { get; set; }
        private string EssJwtToken { get; set; }
        private string FssJwtToken { get; set; }
        private GetProductIdentifiers getproductIdentifier { get; set; }

        private static readonly ESSApiConfiguration ESSAuth = new TestConfiguration().EssConfig;
        private readonly FleetManagerB2BApiConfiguration fleet = new TestConfiguration().fleetManagerB2BConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly POSWebJobApiConfiguration posWebJob = new TestConfiguration().POSWebJobConfig;
        private List<string> productIdentifiers = new();
        private List<string> productIdentifiersAIO = new();
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
            EssJwtToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiI4MGE2YzY4Yi01OWFhLTQ5YTQtOTM5YS03OTY4ZmY3OWQ2NzYiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjgyNTEyOTQxLCJuYmYiOjE2ODI1MTI5NDEsImV4cCI6MTY4MjUxNzY4NSwiYWNyIjoiMSIsImFpbyI6IkFYUUFpLzhUQUFBQVowbVFtN1M1UndITmZhYmV3OVcrb010emlXOHRFVW1kdyt6MEl5b0hnU3ErZ1ZXN1R4am1DK0ZsWjRsNHdqbXd3U3ZYMUNac3RVQ3NDWW9CdGJlak9wUUpHcGdIREdBektNYzMveVNCclhZUDIyeUFxb01kSmNMa3FTWUxpWUJuSlJocUozNjJwUW1NSUgwdFg2NkFMQT09IiwiYW1yIjpbInB3ZCIsInJzYSJdLCJhcHBpZCI6IjgwYTZjNjhiLTU5YWEtNDlhNC05MzlhLTc5NjhmZjc5ZDY3NiIsImFwcGlkYWNyIjoiMCIsImVtYWlsIjoiQXJpdDE0OTc1QG1hc3Rlay5jb20iLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjQ5LjI1NC4yMjkiLCJuYW1lIjoiQXJpdCBTYXJrYXIiLCJvaWQiOiJkNjU0NWZjZS1jYjdkLTQ3MGEtOTU0MC0zZGRiOTE5NzI1MTYiLCJyaCI6IjAuQVZNQVNNbzBrVDFtQlVxV2lqR2tMd3J0UG92R3BvQ3FXYVJKazVwNWFQOTUxbllDQU1ZLiIsInJvbGVzIjpbIkV4Y2hhbmdlU2V0U2VydmljZVVzZXIiXSwic2NwIjoiVXNlci5SZWFkIiwic3ViIjoiTGU4d1hjY3FWWjhWVWRESDNZRHRNdm13Z05meUxUYjAwdUhrRGR3cVlJWSIsInRpZCI6IjkxMzRjYTQ4LTY2M2QtNGEwNS05NjhhLTMxYTQyZjBhZWQzZSIsInVuaXF1ZV9uYW1lIjoiQXJpdDE0OTc1QG1hc3Rlay5jb20iLCJ1dGkiOiJ6cHg0dXhXWFZrdUFwVkticVNNVUFBIiwidmVyIjoiMS4wIn0.jXt4lvdn_S5q2fKbD04_7xcochxrUsguPaKGUG3W6N_xLeQz_CxK1cyowwMRHtmyW-fSS8iMQRsntfMxreFV-iQbjbTk2Vt98qQRwmsssSKnvT6-JvFtVfOmda3qxKk9DMoOrGEeRPibELJfX4jUFiW-yAczdG3vZWBROMVP1KWz39QzhLsQFxS_2tGfAhd4yuZACleQoIRbYEI8jwK2O_s6t9kKOCoBJmEWuCqqSP214AjnPB6K9RiMT1HMPwKhP8_qaw1DRxDNcU-67R4p7eeDo2KGZvjE-7JrcfamlXsWYmzFoDAu4gfxDH7t_PHaceXYbICq7s0Z5Q4GpMgrfA";
            FssJwtToken = "eyJ0eXAiOiJKV1QiLCJhbGciOiJSUzI1NiIsIng1dCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyIsImtpZCI6Ii1LSTNROW5OUjdiUm9meG1lWm9YcWJIWkdldyJ9.eyJhdWQiOiI4MDViZTAyNC1hMjA4LTQwZmItYWI2Zi0zOTljMjY0N2QzMzQiLCJpc3MiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC85MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UvIiwiaWF0IjoxNjgyNTEzMTA3LCJuYmYiOjE2ODI1MTMxMDcsImV4cCI6MTY4MjUxODQ3NywiYWNyIjoiMSIsImFpbyI6IkFYUUFpLzhUQUFBQVZXRG1xbFVmNHhFdUhBZ1RsdTJLSFcxYjAyL1AyM2NXZ2NRUkdiSzBUTWVsODVMWmNiS2F3VlFlNVhvTGU2NFRncHNtQzRNcnZvK0hMMXk0TFo1YUt5QmFmdi9zdVRlcUpvYzExeW00OEwydy9YRXBrcmQ1MEFvRGM2N2pDcnovM2FsM1NOZndKaE05U3BkNlZ0N2s4dz09IiwiYW1yIjpbInB3ZCIsInJzYSJdLCJhcHBpZCI6IjgwNWJlMDI0LWEyMDgtNDBmYi1hYjZmLTM5OWMyNjQ3ZDMzNCIsImFwcGlkYWNyIjoiMCIsImVtYWlsIjoiQXJpdDE0OTc1QG1hc3Rlay5jb20iLCJpZHAiOiJodHRwczovL3N0cy53aW5kb3dzLm5ldC9hZGQxYzUwMC1hNmQ3LTRkYmQtYjg5MC03ZjhjYjZmN2Q4NjEvIiwiaXBhZGRyIjoiMTAzLjQ5LjI1NC4yMjkiLCJuYW1lIjoiQXJpdCBTYXJrYXIiLCJvaWQiOiJkNjU0NWZjZS1jYjdkLTQ3MGEtOTU0MC0zZGRiOTE5NzI1MTYiLCJyaCI6IjAuQVZNQVNNbzBrVDFtQlVxV2lqR2tMd3J0UGlUZ1c0QUlvdnRBcTI4NW5DWkgwelFDQU1ZLiIsInJvbGVzIjpbIkJhdGNoQ3JlYXRlIl0sInNjcCI6IlVzZXIuUmVhZCIsInN1YiI6Inc2OUU3clB6MHZLOFAxcjhaRU9Zci1LeUU1bTd6czU5b0RvTW90RXliOWsiLCJ0aWQiOiI5MTM0Y2E0OC02NjNkLTRhMDUtOTY4YS0zMWE0MmYwYWVkM2UiLCJ1bmlxdWVfbmFtZSI6IkFyaXQxNDk3NUBtYXN0ZWsuY29tIiwidXRpIjoiWGpzeU9XRUNFVVdIWUl5Ny1QTVpBQSIsInZlciI6IjEuMCJ9.ozGotZxi07MSrqnhmfhGoI_b3FsqEwrekOIS8kHBTIl98DrVagTEH6SUgKcd2XTMYjG4YTl2TEN8b-wr6lvs4bnN2woH779FVBjua3ZliGTAT7iPR_CLY16h2tFFMD4xCRmwEKiYqJ7hXoPYFu2_p4piqEyqJOe_W0vd74hsvVWxw1SECW3ZSjp4kY32ZegrFOlC1VscGj4BW2kV-FDmvDQWu_Z61SIqjCnWfRKPHNyyYhd0OR0G23EXlZJXQOZ7WKPwGzs9n9rNWhVzqZ94e0M14YfXuZxUDVAgbitOR4CeVBMI5qdY0y4qgmzUVP71ZsUM-HjfHow7L0xj-H65_g";

            HttpResponseMessage apiResponse = MockHelper.ConfigureFM(posWebJob.MockApiBaseUrl, posWebJob.FMConfigurationValidProductIdentifier);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

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

        [Test]
        public async Task WhenICallTheExchangeSetApiWithValidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GB800001");

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

            //verify model structure
            await apiResponse.CheckModelStructureForSuccessResponseAio();

            productIdentifiersAIO.Clear();
        }

        [Test]
        public async Task WhenICallTheExchangeSetApiWithInvalidAioProductIdentifiers_ThenCorrectResponseIsReturned()
        {
            productIdentifiersAIO.Add("GC800001");

            HttpResponseMessage apiResponse = await getproductIdentifier.GetProductIdentifiersDataAsync(ESSAuth.BaseUrl, productIdentifiersAIO, EssJwtToken);
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);

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
            apiResponse.StatusCode.Should().Be((HttpStatusCode)200);
        }
    }
}
