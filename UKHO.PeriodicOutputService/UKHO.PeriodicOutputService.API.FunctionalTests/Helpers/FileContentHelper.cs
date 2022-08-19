using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static TestConfiguration Config = new TestConfiguration();
        private static FssApiClient FssApiClient = new FssApiClient();
       

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02.zip";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{FolderName}";

                var DownloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, FolderName);

                downloadFolderPath.Add(DownloadedFolder);
            }
            return downloadFolderPath;
        }

        public static void DeleteDirectory(string fileName)
        {
            if (File.Exists(fileName))
            {
                string folder = Path.GetFileName(fileName);
                if (folder.Contains(".zip"))
                {
                    folder = folder.Replace(".zip", "");
                }

                //Delete V01X01.zip/M01XO2.zip/M02XO2.zip file from temp Directory
                if (File.Exists(fileName))
                {
                    File.Delete(fileName);
                }
            }

        }

        public static void DeleteDirectoryForIsoAndSha1Files(string fileName)
        {
            string path = Path.GetTempPath();

            if (Directory.Exists(path) && File.Exists(Path.Combine(path, fileName)))
            {
                string folder = Path.GetFileName(Path.Combine(path, fileName));
                if (folder.Contains(".zip"))
                {
                    folder = folder.Replace(".zip", "");
                   //Delete V01XO1/M01XO2/M02XO2 Directory and sub directories from temp Directory
                   Directory.Delete(Path.Combine(path, folder), true);
                   
                }
                else if(folder.Contains(".iso"))
                {
                    folder = folder.Replace(".iso", "");
                }
                else
                {
                    folder = folder.Replace(".iso.sha1", "");
                }

             
                //Delete V01X01.zip/M01XO2.zip/M02XO2.zip file from temp Directory
                if (File.Exists(Path.Combine(path, fileName)))
                {
                    File.Delete(Path.Combine(path, fileName));
                }
            }

        }

        public static async Task<List<string>> DownloadAndExtractExchangeSetZipFileForLargeMedia(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var folderName = $"M0{mediaNumber}X02";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{folderName}.zip";

                var extractDownloadedFolder = await FssBatchHelper.ExtractDownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, folderName);

                var downloadFolder = FssBatchHelper.RenameFolder(extractDownloadedFolder);
                var downloadFolderPath1 = Path.Combine(Path.GetTempPath(), downloadFolder);
                downloadFolderPath.Add(downloadFolderPath1);
            }
            return downloadFolderPath;
        }

        public static async Task<List<string>> CreateExchangeSetFileForIsoAndSha1Files(string BatchId, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderNameIso = $"M0{mediaNumber}X02.iso";
               
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{FolderNameIso}";

                var DownloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, FolderNameIso);

                var FolderNameSha1 = $"M0{mediaNumber}X02.iso.sha1";
                var downloadFileUrlSha1 = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{FolderNameSha1}";

                var DownloadedFolderSha1 = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, FolderNameSha1);

                downloadFolderPath.Add(DownloadedFolder);
                downloadFolderPath.Add(DownloadedFolderSha1);
            }
            return downloadFolderPath;
        }
    }
}
