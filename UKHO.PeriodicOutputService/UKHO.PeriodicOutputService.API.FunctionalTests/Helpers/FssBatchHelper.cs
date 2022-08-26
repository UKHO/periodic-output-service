using System.IO.Compression;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FssBatchHelper
    {
        private static FssApiClient FssApiClient { get; set; }
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        static FssBatchHelper()
        {
            FssApiClient = new FssApiClient();
        }

        public static async Task<string> DownloadFileForLargeMedia(string downloadFileUrl, string jwtToken)
        {
            string batchId = downloadFileUrl.Split('/')[5];
            string fileName = downloadFileUrl.Split('/')[7];

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

            HttpResponseMessage response = await FssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            Stream stream = await response.Content.ReadAsStreamAsync();

            using (FileStream outputFileStream = new(Path.Combine(batchFolderPath, fileName), FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }
            return batchFolderPath;
        }

        public static string ExtractDownloadedFileForLargeMedia(string downloadPath, string fileName)
        {
            string tempFilePath = Path.Combine(downloadPath, fileName);
            string extractPath = Path.Combine(downloadPath, RenameFolder(tempFilePath));
            ZipFile.ExtractToDirectory(tempFilePath, extractPath);
            return extractPath;
        }

        public static string RenameFolder(string pathInput)
        {
            return Path.GetFileName(pathInput).Replace(".zip", "");
        }
    }
}
