using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Response;

namespace UKHO.FmEssFssMock.API.Services
{
    public class FileShareService
    {
        public BatchResponse CreateBatch(IEnumerable<KeyValuePair<string, string>> attributes, string homeDirectoryPath)
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
                               attributes.Any(a => a.Key.ToLower() == "media type" && a.Value.ToLower() == "zip") &&
                               attributes.Any(a => a.Key.ToLower() == "test" && a.Value.ToLower() == "test"):
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

        public BatchDetail GetBatchDetails(string batchId)
        {
            string path = Path.Combine(Environment.CurrentDirectory, @"Data", batchId);
            List<BatchFile> files = new List<BatchFile>();

            foreach (var filePath in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(filePath);
                files.Add(new BatchFile() { Filename = fileName, Links = new Links() { Get = new Link() { Href = "/batch/" + batchId + "/files/" + fileName } } });
            }
            return new BatchDetail
            {
                BatchId = batchId,
                Files = files
            };
        }

        public byte[] GetFileData(string homeDirectoryPath, string batchId, string fileName)
        {
            byte[] bytes = null;
            var filePath = Path.Combine(homeDirectoryPath, batchId, fileName);

            if (File.Exists(filePath))
            {
                bytes = File.ReadAllBytes(filePath);
            }
            return bytes;
        }

        public bool UploadBlockOfFile(string batchid, string homeDirectoryPath, string fileName)
        {
            string filePath = Path.Combine(homeDirectoryPath, batchid, fileName);
            return FileHelper.CheckBatchWithFileExist(filePath);
        }

        public bool CheckBatchWithFileExist(string batchid, string fileName, string homeDirectoryPath)
        {
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchid, fileName);

            return FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckBatchWithFileExist(batchFolderPath);
        }

        public bool AddFile(string batchid, string fileName, string homeDirectoryPath)
        {
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchid);

            if (FileHelper.CheckFolderExists(batchFolderPath))
            {
                string srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchid, fileName);
                string destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchid), fileName);
                File.Copy(srcFile, destFile, true);
                return true;
            }
            return false;
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
