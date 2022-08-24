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

        public static async Task<string> DownloadedFolderForLargeFiles(string downloadFileUrl, string jwtToken, string folderName)
        {
            string LargeFolderName = folderName;
            string tempFilePath = Path.Combine(Path.GetTempPath(), LargeFolderName);

            var response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }
            return tempFilePath;
        }

        public static async Task<string> ExtractDownloadedFolderForLargeFiles(string downloadFileUrl, string jwtToken, string folderName)
        {
            string largeFolderName = folderName + ".zip";
            string tempFilePath = Path.Combine(Path.GetTempPath(), largeFolderName);
            string zipPath = await DownloadedFolderForLargeFiles(downloadFileUrl, jwtToken, largeFolderName);

            string extractPath = Path.GetTempPath() + RenameFolder(tempFilePath);

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return extractPath;
        }

        public static string RenameFolder(string pathInput)
        {
            return Path.GetFileName(pathInput).Replace(".zip", "");
        }
    }
}
