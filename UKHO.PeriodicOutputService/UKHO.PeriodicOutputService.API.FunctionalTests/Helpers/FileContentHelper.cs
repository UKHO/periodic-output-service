using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using static UKHO.PeriodicOutputService.API.FunctionalTests.Helpers.TestConfiguration;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration Config = new();
        private static readonly POSFileDetails posDetails = new TestConfiguration().posFileDetails;

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string folderName = $"M0{mediaNumber}X02.zip";
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}";

                string downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, FssJwtToken);

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

            string mediaType = batchDetailsResponse.attributes[6].value;

            if (mediaType.Equals("Zip"))
            {
                string fileName = batchDetailsResponse.files[0].filename;
                if (fileName.Equals($"{posDetails.UpdateExchangeSet}"))
                {
                    string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{posDetails.UpdateExchangeSet}";
                    string zipPath = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                    string extractDownloadedFolder = FssBatchHelper.ExtractDownloadedFileForLargeMedia(zipPath, posDetails.UpdateExchangeSet);
                    downloadFolderPath.Add(extractDownloadedFolder);
                }
                else
                {
                    for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
                    {
                        string zipFileName = $"M0{mediaNumber}X02.zip";
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

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string folderNameIso = $"M0{mediaNumber}X02.iso";

                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{folderNameIso}";

                string downloadedFolder = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);

                string FolderNameSha1 = $"M0{mediaNumber}X02.iso.sha1";
                string downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{FolderNameSha1}";

                string downloadedFolderSha1 = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrlSha1, fssJwtToken);

                downloadFolderPath.Add(downloadedFolder);
                downloadFolderPath.Add(downloadedFolderSha1);
            }
            return downloadFolderPath;
        }
        public static async Task<List<string>> DownloadCatalogueXmlOrEncUpdatesListCsvFileForLargeMedia(string batchId, string fssJwtToken, dynamic batchDetailsResponse)
        {
            List<string> downloadFolderPath = new();
            string responseContent = batchDetailsResponse.attributes[5].value;
            if (responseContent.Equals("Catalogue"))
            {
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{posDetails.AVCSCatalogueFileName}";
                string downloadFile = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                downloadFolderPath.Add(downloadFile);
            }
            else
            {
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{batchId}/files/{posDetails.EncUpdateListFileName}";
                string downloadFile = await FssBatchHelper.DownloadFileForLargeMedia(downloadFileUrl, fssJwtToken);
                downloadFolderPath.Add(downloadFile);
            }
            return downloadFolderPath;
        }
    }
}
