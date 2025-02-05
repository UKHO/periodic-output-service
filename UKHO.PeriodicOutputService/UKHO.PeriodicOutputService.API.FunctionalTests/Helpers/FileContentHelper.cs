using System.Globalization;
using System.Net;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration s_config = new();
        private static readonly POSFileDetails s_posDetails = new TestConfiguration().posFileDetails;
        private static readonly string s_weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString("00");
        private static readonly string s_currentYear = DateTime.UtcNow.ToString("yy");

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string batchId, string fssJwtToken)
        {
            List<string> downloadFolderPath = [];

            for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
            {
                string zipFileName = string.Format(s_posDetails.PosAvcsZipFileName, dvdNumber, s_weekNumber, s_currentYear);
                string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";

                string downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

                downloadFolderPath.Add(downloadedFolder);
            }
            return downloadFolderPath;
        }

        public static void DeleteTempDirectory(string tempFolder)
        {
            string path = Path.GetTempPath() + tempFolder;

            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }

        public static async Task<List<string>> DownloadAndExtractExchangeSetZipFileForLargeMedia(string batchId, string fssJwtToken, dynamic batchDetailsResponse)
        {
            List<string> downloadFolderPath = [];

            string mediaType = batchDetailsResponse.attributes[5].value;

            if (mediaType.ToLower().Equals("zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals(string.Format(s_posDetails.PosUpdateZipFileName, s_weekNumber, s_currentYear)))
                {
                    string zipFileName = string.Format(s_posDetails.PosUpdateZipFileName, s_weekNumber, s_currentYear);
                    string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";
                    string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                    string extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, zipFileName);
                    downloadFolderPath.Add(extractDownloadedFolder);
                }
                else
                {
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        string zipFileName = string.Format(s_posDetails.PosAvcsZipFileName, dvdNumber, s_weekNumber, s_currentYear);
                        string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";

                        string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                        string extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, zipFileName);
                        downloadFolderPath.Add(extractDownloadedFolder);
                    }
                }
            }
            return downloadFolderPath;
        }
        public static async Task<List<string>> CreateExchangeSetFileForIsoAndSha1Files(string batchId, string fssJwtToken)
        {
            List<string> downloadFolderPath = [];

            for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
            {
                string folderNameIso = string.Format(s_posDetails.PosAvcsIsoFileName, dvdNumber, s_weekNumber, s_currentYear);

                string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderNameIso}";

                string downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

                string FolderNameSha1 = string.Format(s_posDetails.PosAvcsIsoSha1FileName, dvdNumber, s_weekNumber, s_currentYear);
                string downloadFileUrlSha1 = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{FolderNameSha1}";

                string downloadedFolderSha1 = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrlSha1, fssJwtToken);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
        public static async Task<List<string>> DownloadCatalogueXmlOrEncUpdatesListCsvFileForLargeMedia(string batchId, string fssJwtToken, dynamic batchDetailsResponse)
        {
            string responseContent = batchDetailsResponse.attributes[4].value;

            string downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/";
            downloadFileUrl += responseContent.Equals("Catalogue")
                ? s_posDetails.AVCSCatalogueFileName
                : s_posDetails.EncUpdateListFileName;

            string downloadFile = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

            List<string> downloadFolderPath =
            [
                downloadFile
            ];

            return downloadFolderPath;
        }

        public static async Task VerifyPosBatches(string fssJwtToken)
        {
            string[] posBatchId = [s_posDetails.IsoSha1BatchId, s_posDetails.ZipFilesBatchId, s_posDetails.CatalogueBatchId, s_posDetails.UpdateExchangeSetBatchId, s_posDetails.EncUpdateListCsvBatchId];

            foreach (string posBatchIdNumber in posBatchId)
            {
                HttpResponseMessage responseMessage = await FssBatchHelper.PosBatchesVerification(fssJwtToken, posBatchIdNumber);
                Assert.That(responseMessage.StatusCode, Is.EqualTo((HttpStatusCode)404));
            }
        }

        public static async Task<string> DownloadAndExtractAioZip(string fssJwtToken, string batchId)
        {
            var fileName = $"AIO_CD_WK{s_weekNumber}_{s_currentYear}";
            var downloadFileUrl = $"{s_config.FssConfig.BaseUrl}/batch/{batchId}/files/{fileName}.zip";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedAioFolder(downloadFileUrl.ToString(), fssJwtToken);

            return extractDownloadedFolder;
        }
    }
}
