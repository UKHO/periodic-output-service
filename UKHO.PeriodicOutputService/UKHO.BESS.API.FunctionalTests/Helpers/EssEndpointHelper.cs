using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.BESS.API.FunctionalTests.Models;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public static class EssEndpointHelper
    {
        static readonly HttpClient httpClient = new();
        private static string? uri;
        private static TestConfiguration configs = new();

        /// <summary>
        /// This Method is used to execute ESS Product Identifier endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the ESS baseUrl</param>
        /// <param name="productIdentifier">Sets the products to get data</param>
        /// <param name="exchangeSetStandard">Sets the type of ExchangeStandard required</param>
        /// <param name="validUri">Sets the valid or invalid uri. Default is true</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ProductIdentifiersEndpoint(string? baseUrl, List<string> productIdentifier, string? exchangeSetStandard = "s63", bool validUri = true)
        {
            uri = validUri ? $"{baseUrl}/productData/productIdentifiers?exchangeSetStandard={exchangeSetStandard}" : $"{baseUrl}/productData/productIdentifiers123";

            string payloadJson = JsonConvert.SerializeObject(productIdentifier);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This Method is used to execute ESS Product Version endpoint
        /// </summary>
        /// <param name="baseUrl">Sets the ESS baseUrl</param>
        /// <param name="productVersion">Sets the product and its version to get data</param>
        /// <param name="exchangeSetStandard">Sets the type of ExchangeStandard required</param>
        /// <param name="validUri">Sets the valid or invalid uri. Default is true</param>
        /// <returns></returns>
        public static async Task<HttpResponseMessage> ProductVersionsEndpoint(string? baseUrl, List<ProductVersionModel> productVersion, string? exchangeSetStandard = "s63", bool validUri = true)
        {
            uri = validUri ? $"{baseUrl}/productData/productVersions?exchangeSetStandard={exchangeSetStandard}" : $"{baseUrl}/productData/productIdentifiers123";

            string payloadJson = JsonConvert.SerializeObject(productVersion);

            using var httpRequestMessage = new HttpRequestMessage(HttpMethod.Post, uri)
            { Content = new StringContent(payloadJson, Encoding.UTF8, "application/json") };
            return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
        }

        /// <summary>
        /// This Method is used to check the Batch Status and Download ES
        /// </summary>
        /// <param name="batchId">Provide the BatchId of the requested Product</param>
        /// <param name="isPermitFileRequested">Checks if permit is requested to download</param>
        /// <param name="keyFileType">Sets the key file type</param>
        /// <param name="isBessBatch">Checks if bess batch is getting created or not</param>
        /// <returns></returns>
        public static async Task<string> CreateExchangeSetFile(string batchId, bool isPermitFileRequested = false, string? keyFileType = null, bool isBessBatch = false)
        {
            string finalBatchStatusUrl = $"{configs.fssConfig.BaseUrl}/batch/{batchId}/status";
            string batchStatus = await FssBatchHelper.CheckBatchIsCommitted(finalBatchStatusUrl);

            Assert.That(batchStatus, Does.Contain("Committed"));
            string downloadFileUrl = $"{configs.fssConfig.BaseUrl}/batch/{batchId}/files/{configs.exchangeSetDetails.ExchangeSetFileName}";
            if (isBessBatch)
            {
                downloadFileUrl = $"{configs.fssConfig.BaseUrl}/batch/{batchId}/files/{configs.exchangeSetDetails.BessExchangeSetFileName}";
            }
            string extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolder(downloadFileUrl, isPermitFileRequested, keyFileType);

            return extractDownloadedFolder;
        }
    }
}
