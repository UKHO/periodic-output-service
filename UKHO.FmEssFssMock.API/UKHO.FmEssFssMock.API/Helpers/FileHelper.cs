using System.Security.Cryptography;
using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Helpers
{
    public static class FileHelper
    {
        public static T ReadJsonFile<T>(string filePathWithFileName)
        {
            T? response = JsonConvert.DeserializeObject<T>(File.ReadAllText(Path.Combine(Directory.GetCurrentDirectory(), filePathWithFileName)));
            return response;
        }

        public static void CheckAndCreateFolder(string folderPath)
        {
            if (Directory.Exists(folderPath))
            {
                Directory.Delete(folderPath, true);
            }
            Directory.CreateDirectory(folderPath);
        }

        public static bool CheckBatchWithFileExist(string filePathWithFileName)
        {
            return ValidateFilePath(filePathWithFileName) && File.Exists(filePathWithFileName);
        }

        public static bool CheckFolderExists(string filePath)
        {
            return ValidateFilePath(filePath) && Directory.Exists(filePath);
        }

        public static bool ValidateFilePath(string filePath)
        {
            return !string.IsNullOrEmpty(filePath);
        }

        public static bool CleanUp(string filePath)
        {
            if (CheckFolderExists(filePath))
            {
                DirectoryInfo directoryInfo = new(filePath);
                directoryInfo.Delete(true);
                return true;
            }
            return false;
        }

        public static string GetFileMD5(FileInfo fileInfo)
        {
            using Stream? fileStream = fileInfo.OpenRead();
            using var md5 = MD5.Create();
            byte[] fileMd5Hash = md5.ComputeHash(fileStream);
            return Convert.ToBase64String(fileMd5Hash);
        }

        public static void CopyAllFiles(string srcFile, string destFile)
        {
            // Ensure the destination directory exists
            Directory.CreateDirectory(destFile);

            // Get all files from the source directory
            string[] files = Directory.GetFiles(srcFile);

            foreach (string filePath in files)
            {
                // Get the file name
                string fileName = Path.GetFileName(filePath);

                // Create the destination file path
                string destFilePath = Path.Combine(destFile, fileName);

                // Copy the file
                File.Copy(filePath, destFilePath, overwrite: true);
            }
        }
    }
}
