using System.IO.Compression;
using Newtonsoft.Json;
using NUnit.Framework;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FssBatchHelper
    {
        private static FssApiClient FssApiClient { get; set; }
        static FSSApiConfiguration FSSAuth = new TestConfiguration().FssConfig;
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        static TestConfiguration EssConfig { get; set; }

        static FssBatchHelper()
        {
            FssApiClient = new FssApiClient();
            EssConfig = new TestConfiguration();
        }

        public static async Task<string> CheckBatchIsCommitted(string batchStatusUri, string jwtToken)
        {
            string batchStatus = "";
            var startTime = DateTime.UtcNow;

            while (DateTime.UtcNow - startTime   < TimeSpan.FromMinutes(FSSAuth.BatchCommitWaitTime))
            {
                await Task.Delay(5000);
                var batchStatusResponse = await FssApiClient.GetBatchStatusAsync(batchStatusUri, jwtToken);
                Assert.That((int)batchStatusResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {batchStatusResponse.StatusCode}, instead of the expected status 200 for url {batchStatusUri}.");

                var batchStatusResponseObj = JsonConvert.DeserializeObject<ResponseBatchStatusModel>(await batchStatusResponse.Content.ReadAsStringAsync());
                batchStatus = batchStatusResponseObj.Status;

                if (batchStatus.Equals("Committed"))
                    break;
            }

            return batchStatus;
        }

        public static async Task<string> DownloadFileForLargeMedia(string downloadFileUrl, string jwtToken)
        {
            var batchId = downloadFileUrl.Split('/')[5];
            var fileName = downloadFileUrl.Split('/')[7];
            
            string posFolderPath = Path.Combine(Path.GetTempPath(), posDetails.TempFolderName);
            if (!Directory.Exists(posFolderPath))
            {
                Directory.CreateDirectory(posFolderPath);
            }

            string batchFolderPath = Path.Combine(posFolderPath, batchId);
            if (!Directory.Exists(batchFolderPath))
            {
                Directory.CreateDirectory(batchFolderPath);
            }

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new FileStream(Path.Combine(batchFolderPath, fileName), FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }
            return batchFolderPath;
        }

        public static string ExtractDownloadedFileForLargeMedia(string downloadPath, string fileName)
        {
            string tempFilePath = Path.Combine(downloadPath, fileName);
            string extractPath = Path.Combine(downloadPath,RenameFolder(tempFilePath));
            ZipFile.ExtractToDirectory(tempFilePath, extractPath);
            return extractPath;
        }

        public static string RenameFolder(string pathInput)
        {
            return Path.GetFileName(pathInput).Replace(".zip", "");
        }
    }
}
