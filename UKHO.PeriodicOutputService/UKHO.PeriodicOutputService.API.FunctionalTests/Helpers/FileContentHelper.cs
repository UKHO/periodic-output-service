using System.Globalization;
using NUnit.Framework;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration Config = new();
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static readonly string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
        private static readonly string currentYear = DateTime.UtcNow.ToString("yy");

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string batchId, string fssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
            {
                string zipFileName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";

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
            List<string> downloadFolderPath = new();

            string mediaType = batchDetailsResponse.attributes[5].value;

            if (mediaType.ToLower().Equals("zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals(string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear)))
                {
                    string zipFileName = string.Format(posDetails.PosUpdateZipFileName, weekNumber, currentYear);
                    string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";
                    string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                    string extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, zipFileName);
                    downloadFolderPath.Add(extractDownloadedFolder);
                }
                else
                {
                    for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
                    {
                        string zipFileName = string.Format(posDetails.PosAvcsZipFileName, dvdNumber, weekNumber, currentYear);
                        string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";

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
            List<string> downloadFolderPath = new();

            for (int dvdNumber = 1; dvdNumber <= 2; dvdNumber++)
            {
                string folderNameIso = string.Format(posDetails.PosAvcsIsoFileName, dvdNumber, weekNumber, currentYear);

                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderNameIso}";

                string downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

                string FolderNameSha1 = string.Format(posDetails.PosAvcsIsoSha1FileName, dvdNumber, weekNumber, currentYear);
                string downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{FolderNameSha1}";

                string downloadedFolderSha1 = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrlSha1, fssJwtToken);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
        public static async Task<List<string>> DownloadCatalogueXmlOrEncUpdatesListCsvFileForLargeMedia(string batchId, string fssJwtToken, dynamic batchDetailsResponse)
        {
            string responseContent = batchDetailsResponse.attributes[4].value;

            string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/";
            downloadFileUrl += responseContent.Equals("Catalogue")
                ? posDetails.AVCSCatalogueFileName
                : posDetails.EncUpdateListFileName;

            string downloadFile = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

            List<string> downloadFolderPath = new()
            {
                downloadFile
            };

            return downloadFolderPath;
        }

        public static async Task VerifyPosBatches(string fssJwtToken)
        {
            string[] posBatchId = { posDetails.IsoSha1BatchId, posDetails.ZipFilesBatchId ,posDetails.CatalogueBatchId, posDetails.UpdateExchangeSetBatchId , posDetails.EncUpdateListCsvBatchId };

            foreach(string posBatchIdNumber in posBatchId)
            {
                HttpResponseMessage responseMessage = await FssBatchHelper.PosBatchesVerification(fssJwtToken, posBatchIdNumber);
                Assert.Equals(responseMessage.StatusCode, (System.Net.HttpStatusCode)404);
            }
        }

        public static async Task <string> DownloadAndExtractAioZip(string FssJwtToken, string batchId)
        {
            string filename = "AIO_S631-1_CD_WK" + weekNumber + "_" + currentYear;
            var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{filename}.zip";

            var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedAioFolder(downloadFileUrl.ToString(), FssJwtToken);

            return extractDownloadedFolder;
        }
    }
}
