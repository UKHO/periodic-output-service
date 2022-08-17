using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UKHO.ExchangeSetService.API.FunctionalTests.Models;
using System.Net.Http;
using UKHO.PeriodicOutputService.API.FunctionalTests.Helpers;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.ExchangeSetService.API.FunctionalTests.Helpers
{
    public static class FileContentHelper
    {
        private static TestConfiguration Config = new TestConfiguration();
        private static FssApiClient FssApiClient = new FssApiClient();
       

        public static async Task<List<string>> CreateExchangeSetFileForLargeMedia(HttpResponseMessage apiEssResponse, string FssJwtToken)
        {
            List<string> downloadFolderPath = new List<string>();
            Assert.That((int)apiEssResponse.StatusCode, Is.EqualTo(200), $"Incorrect status code is returned {apiEssResponse.StatusCode}, instead of the expected status 200.");

            for (int mediaNumber = 1; mediaNumber <= 2; mediaNumber++)
            {
                var FolderName = $"M0{mediaNumber}X02";
                var downloadFileUrl = $"{Config.FssConfig.BaseUrl}/batch/2270F318-639C-4E64-A0C0-CADDD5F4EB05/files/{FolderName}.zip";

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
