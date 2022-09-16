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
                using (var output = File.OpenWrite(uploadBlockFilePath))
                {
                    output.Write(content, 0, content.Length);
                }
            }
        }

        public static bool CheckBatchWithFileExist(string filePathWithFileName)
        {
            if (ValidateFilePath(filePathWithFileName))
            {
                return File.Exists(filePathWithFileName);
            }
            return false;
        }

        public static bool CheckFolderExists(string filePath)
        {
            if (ValidateFilePath(filePath))
            {
                return Directory.Exists(filePath);
            }
            return false;
        }

        public static bool ValidateFilePath(string filePath)
        {
            return !string.IsNullOrEmpty(filePath);
        }

        public static bool CleanUp(string filePath)
        {
            if (CheckFolderExists(filePath))
            {
                DirectoryInfo di = new DirectoryInfo(filePath);
                di.Delete(true);
                return true;
            }
            return false;
        }
    }
}
