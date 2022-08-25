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
                var folderName = $"M0{mediaNumber}X02.zip";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}";

                var downloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderName);

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

        public static async Task<List<string>> DownloadAndExtractExchangeSetZipFileForLargeMedia(string batchId, string fssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderName = $"M0{mediaNumber}X02";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderName}.zip";

                var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolderForLargeFiles(downloadFileUrl, fssJwtToken, folderName);

                var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
                var tmpDownloadFolderPath = Path.Combine(Path.GetTempPath(), downloadFolder);
                downloadFolderPath.Add(tmpDownloadFolderPath);
            }
            return downloadFolderPath;
        }
        public static async Task<List<string>> CreateExchangeSetFileForIsoAndSha1Files(string batchId, string fssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderNameIso = $"M0{mediaNumber}X02.iso";

                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderNameIso}";

                var downloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, fssJwtToken, folderNameIso);

                var FolderNameSha1 = $"M0{mediaNumber}X02.iso.sha1";
                var downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{FolderNameSha1}";

                var downloadedFolderSha1 = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrlSha1, fssJwtToken, FolderNameSha1);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
    }
}
