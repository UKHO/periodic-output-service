using System.IO.Compression;
using System.Net;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FssBatchHelper
    {
        private static readonly FssApiClient s_fssApiClient;
        private static readonly POSFileDetails s_posDetails = new TestConfiguration().posFileDetails;
        private static readonly TestConfiguration s_config = new();
        private static readonly string s_weekNumberAio;
        private static readonly string s_currentYearShortAio;

        static FssBatchHelper()
        {
            s_fssApiClient = new FssApiClient();
            (s_weekNumberAio, _, s_currentYearShortAio) = CommonHelper.GetCurrentWeekAndYearAio();
        }

        public static async Task<string> DownloadFileForLargeMedia(string downloadFileUrl, string jwtToken)
        {
            string batchId = downloadFileUrl.Split('/')[5];
            string fileName = downloadFileUrl.Split('/')[7];

            string posFolderPath = Path.Combine(Path.GetTempPath(), s_posDetails.TempFolderName);
            if (!Directory.Exists(posFolderPath))
            {
                Directory.CreateDirectory(posFolderPath);
            }

            string batchFolderPath = Path.Combine(posFolderPath, batchId);
            if (!Directory.Exists(batchFolderPath))
            {
                Directory.CreateDirectory(batchFolderPath);
            }

            HttpResponseMessage response = await s_fssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.That(response.StatusCode, Is.EqualTo((HttpStatusCode)200));
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

        public static async Task<HttpResponseMessage> VerifyErrorTxtExist(string jwtToken)
        {
            string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{s_posDetails.InvalidProductIdentifierBatchId}/files/error.txt";
            HttpResponseMessage response = await s_fssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            return response;
        }

        public static async Task<HttpResponseMessage> PosBatchesVerification(string jwtToken, string batchId)
        {
            string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}";
            HttpResponseMessage response = await s_fssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            return response;
        }

        public static async Task<string> ExtractDownloadedAioFolder(string downloadFileUrl, string jwtToken)
        {
            var fileName = $"AIO_CD_WK{s_weekNumberAio}_{s_currentYearShortAio}.zip";
            var posFolderPath = Path.Combine(Path.GetTempPath(), s_posDetails.TempFolderName);

            if (!Directory.Exists(posFolderPath))
            {
                Directory.CreateDirectory(posFolderPath);
            }

            var tempFilePath = Path.Combine(posFolderPath, fileName);

            var response = await s_fssApiClient.GetFileDownloadAsync(downloadFileUrl, accessToken: jwtToken);
            Assert.That((int)response.StatusCode, Is.EqualTo(200), $"Incorrect status code File Download api returned {response.StatusCode} for the url {downloadFileUrl}, instead of the expected 200.");

            var stream = await response.Content.ReadAsStreamAsync();

            using (var outputFileStream = new FileStream(tempFilePath, FileMode.Create))
            {
                stream.CopyTo(outputFileStream);
            }

            var zipPath = tempFilePath;
            var extractPath = Path.Combine(posFolderPath, RenameFolder(tempFilePath));

            ZipFile.ExtractToDirectory(zipPath, extractPath);

            return extractPath;
        }
    }
}
