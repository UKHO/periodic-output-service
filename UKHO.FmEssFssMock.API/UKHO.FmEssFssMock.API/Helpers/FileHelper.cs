﻿using System.Security.Cryptography;
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

        public static void CreateFileContentWithBytes(string uploadBlockFilePath, byte[] content)
        {
            if (ValidateFilePath(uploadBlockFilePath))
            {
                using (FileStream output = File.OpenWrite(uploadBlockFilePath))
                {
                    output.Write(content, 0, content.Length);
                }
            }
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
            byte[]? fileMd5Hash = md5.ComputeHash(fileStream);
            return Convert.ToBase64String(fileMd5Hash);
        }
    }
}
