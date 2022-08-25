using System.IO.Compression;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FssBatchHelper
    {
        private static FssApiClient FssApiClient { get; set; }

        static FssBatchHelper()
        {
            FssApiClient = new FssApiClient();
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
