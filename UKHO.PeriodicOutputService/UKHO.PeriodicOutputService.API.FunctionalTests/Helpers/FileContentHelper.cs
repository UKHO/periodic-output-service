using NUnit.Framework;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static readonly TestConfiguration Config = new();

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(HttpResponseMessage apiEssResponse, string FssJwtToken, string BatchId)
        {
            List<string> downloadFolderPath = new();
            Assert.That((int)apiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                string FolderName = $"M0{mediaNumber}X02";
                string downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/{BatchId}/files/{FolderName}.zip";

                string DownloadedFolder = await FssBatchHelper.DownloadedFolderForLargeFiles(downloadFileUrl, FssJwtToken, FolderName);

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

        public static bool CheckforFileExist(string filePath, string fileName)
        {
            return (Directory.Exists(filePath) && File.Exists(Path.Combine(filePath, fileName)));
        }

        public static bool CheckforFolderExist(string filePath, string folderName)
        {
            return Directory.Exists(Path.Combine(filePath, folderName));
        }

    }
}
