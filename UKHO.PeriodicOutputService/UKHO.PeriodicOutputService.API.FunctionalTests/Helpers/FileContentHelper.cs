using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static TestConfiguration Config = new TestConfiguration();
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderName = $"M0{mediaNumber}X02.zip";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}";

                var downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, FssJwtToken);

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
            List<string> downloadFolderPath = new List<string>();
            string zipFileName;

            string mediaType = batchDetailsResponse.attributes[1].value;

            if (mediaType.Equals("Zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals($"{posDetails.UpdateExchangeSet}"))
                {
                    var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{posDetails.UpdateExchangeSet}";
                    string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                    var extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, posDetails.UpdateExchangeSet);
                    downloadFolderPath.Add(extractDownloadedFolder);
                }
                else
                {
                    for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
                    {
                        zipFileName = $"M0{mediaNumber}X02.zip";
                        var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{zipFileName}";

                        string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                        var extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, zipFileName);
                        downloadFolderPath.Add(extractDownloadedFolder);
                    }
                }
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

                var downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

                var FolderNameSha1 = $"M0{mediaNumber}X02.iso.sha1";
                var downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{FolderNameSha1}";

                var downloadedFolderSha1 = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrlSha1, fssJwtToken);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
    }
}
