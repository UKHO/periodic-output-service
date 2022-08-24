using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration Config = new();

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string folderName = $"M0{mediaNumber}X02.zip";
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}";

                string downloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderName);

                downloadFolderPath.Add(downloadedFolder);
            }
            return downloadFolderPath;
        }

        public static void DeleteZipIsoSha1Files(string fullFileName)
        {
            string path = Path.GetTempPath();

            if (Directory.Exists(Path.Combine(path, fullFileName)))
            {
                Directory.Delete(Path.Combine(path, fullFileName), true);
            }

            File.Delete(Path.Combine(path, fullFileName + ".zip"));
            File.Delete(Path.Combine(path, fullFileName + ".iso"));
            File.Delete(Path.Combine(path, fullFileName + ".iso.sha1"));
            File.Delete(Path.Combine(path, fullFileName));
        }

        public static async Task<List<string>> DownloadAndExtractExchangeSetZipFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string folderName = $"M0{mediaNumber}X02";
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}.zip";

                string extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderName);

                string downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
                string tmpDownloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);
                downloadFolderPath.Add(tmpDownloadFolderPath);
            }
            return downloadFolderPath;
        }

        public static async Task<List<string>> CreateExchangeSetFileForIsoAndSha1Files(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string folderNameIso = $"M0{mediaNumber}X02.iso";

                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderNameIso}";

                string downloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderNameIso);

                string FolderNameSha1 = $"M0{mediaNumber}X02.iso.sha1";
                string downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{FolderNameSha1}";

                string downloadedFolderSha1 = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrlSha1, FssJwtToken, FolderNameSha1);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
    }
}
