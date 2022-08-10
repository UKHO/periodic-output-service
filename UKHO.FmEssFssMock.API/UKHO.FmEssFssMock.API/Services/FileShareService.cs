using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Response;

namespace UKHO.FmEssFssMock.API.Services
{
    public class FileShareService
    {
        private readonly IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration;

        public FileShareService(IOptions<FileShareServiceConfiguration> fileShareServiceConfiguration)
        {
            this.fileShareServiceConfiguration = fileShareServiceConfiguration;
        }

        public BatchResponse CreateBatch(IEnumerable<KeyValuePair<String, string>> attributes, string homeDirectoryPath)
        {
            string batchId = string.Empty;

            switch (attributes.Any())
            {
                case true when attributes.Any(a => a.Key.ToLower() == "exchange set type" && a.Value.ToLower() == "base") &&
                               attributes.Any(a => a.Key.ToLower() == "media type" && a.Value.ToLower() == "dvd"):
                    batchId = "F9523D33-EF12-4CC1-969D-8A95F094A48B";
                    break;
                case true when attributes.Any(a => a.Key.ToLower() == "exchange set type" && a.Value.ToLower() == "base") &&
                               attributes.Any(a => a.Key.ToLower() == "media type" && a.Value.ToLower() == "zip"):
                    batchId = "483AA1B9-8A3B-49F2-BAE9-759BB93B04D1";
                    break;
                case true when attributes.Any(a => a.Key.ToLower() == "exchange set type" && a.Value.ToLower() == "update") &&
                               attributes.Any(a => a.Key.ToLower() == "media type" && a.Value.ToLower() == "zip"):
                    batchId = "90FCDFA0-8229-43D5-B059-172491E5402B";
                    break;
                case true when attributes.Any(a => a.Key.ToLower() == "catalogue type" && a.Value.ToLower() == "xml") &&
                               attributes.Any(a => a.Key.ToLower() == "content" && a.Value.ToLower() == "catalogue"):
                    batchId = "BECE0A26-867C-4EA6-8ECE-98AFA246A00E";
                    break;
                default:
                    batchId = "2270F318-639C-4E64-A0C0-CADDD5F4EB05";
                    break;
            }

            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            FileHelper.CheckAndCreateFolder(batchFolderPath);
            return new BatchResponse() { BatchId = Guid.Parse(batchId) };
        }

        public SearchBatchResponse GetBatches(string filter)
        {
            var response = FileHelper.ReadJsonFile<SearchBatchResponse>(fileShareServiceConfiguration.Value.FileDirectoryPath + fileShareServiceConfiguration.Value.ScsResponseFile);
            if (filter.Contains("README.TXT", StringComparison.OrdinalIgnoreCase))
            {
                response.Entries.RemoveRange(1, response.Entries.Count - 1);
            }
            return response;
        }

        public byte[] GetFileData(string homeDirectoryPath, string batchId, string filesName)
        {
            string fileType = Path.GetExtension(filesName);
            string[] filePaths;
            byte[] bytes = null;
            var setZipPath = Path.Combine(homeDirectoryPath, batchId);
            if (!string.IsNullOrEmpty(setZipPath) && FileHelper.ValidateFilePath(setZipPath) && Directory.Exists(setZipPath)
                && FileHelper.ValidateFilePath(Directory.GetFiles(setZipPath, filesName).FirstOrDefault()) && string.Equals("V01X01.zip", filesName, StringComparison.OrdinalIgnoreCase))
            {
                filePaths = Directory.GetFiles(setZipPath, filesName);
            }
            else if (FileHelper.ValidateFilePath(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && Directory.Exists(fileShareServiceConfiguration.Value.FileDirectoryPathForENC) && !string.Equals("README.TXT", filesName, StringComparison.OrdinalIgnoreCase))
            {
                filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForENC, string.Equals(fileType, ".TXT", StringComparison.OrdinalIgnoreCase) ? "*.TXT" : "*.000");
            }
            else
            {
                filePaths = Directory.GetFiles(fileShareServiceConfiguration.Value.FileDirectoryPathForReadme, filesName);
            }
            if (filePaths != null && filePaths.Any())
            {
                string filePath = filePaths[0];
                bytes = File.ReadAllBytes(filePath);
            }
            return bytes;
        }

        public bool UploadBlockOfFile(string batchid, string homeDirectoryPath, string fileName)
        {
            string uploadBlockFolderPath = Path.Combine(homeDirectoryPath, batchid);

            if (FileHelper.CheckFolderExists(uploadBlockFolderPath))
            {
                string srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchid, fileName);
                string destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchid), fileName);
                File.Copy(srcFile, destFile, true);
                return true;
            }
            return false;
        }

        public bool CheckBatchWithFileExist(string batchid, string fileName, string homeDirectoryPath)
        {
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchid, fileName);

            return FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckBatchWithFileExist(batchFolderPath);
        }

        public bool CheckBatchFolderExists(string batchid, string homeDirectoryPath)
        {
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchid);

            return FileHelper.CheckFolderExists(batchFolderPath);
        }

        public BatchStatusResponse GetBatchStatus(string batchId, string homeDirectoryPath)
        {
            BatchStatusResponse batchStatusResponse = new BatchStatusResponse();
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            if (FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckFolderExists(batchFolderPath))
            {
                batchStatusResponse.BatchId = batchId;
                batchStatusResponse.Status = "Committed";
            }
            return batchStatusResponse;
        }

        public bool CleanUp(List<string> batchId, string homeDirectoryPath)
        {
            bool deleteFlag = false;
            foreach (var item in batchId)
            {
                string exchangeSetZipFolderPath = Path.Combine(homeDirectoryPath, item);
                var response = FileHelper.CleanUp(exchangeSetZipFolderPath);
                if (response)
                {
                    deleteFlag = true;
                }
            }
            return deleteFlag;
        }
    }
}
