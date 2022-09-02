using System.Globalization;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration Config = new();
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;
        private static string weekNumber = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString().PadLeft(2, '0');
        private static string currentYear = DateTime.UtcNow.ToString("yy");

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

            string mediaType = batchDetailsResponse.attributes[1].value;
            
            if (mediaType.Equals("Zip"))
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
    }
}
