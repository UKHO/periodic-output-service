using System.Globalization;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class FileShareService
    {
        public BatchResponse CreateBatch(IEnumerable<KeyValuePair<string, string>> attributes, string homeDirectoryPath)
        {
            string batchId = string.Empty;

            string attributeValue = attributes.Where(a => a.Key.ToLower() == "batch type").FirstOrDefault().Value.ToLower();
            Enum.TryParse(attributeValue, true, out Batch batchType);
            batchId = EnumHelper.GetEnumDescription(batchType);

            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            FileHelper.CheckAndCreateFolder(batchFolderPath);
            return new BatchResponse() { BatchId = Guid.Parse(batchId) };
        }

        public BatchDetail GetBatchDetails(string batchId)
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            int currentWeek = cultureInfo.Calendar.GetWeekOfYear(DateTime.UtcNow, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
            string currentYear = DateTime.UtcNow.Year.ToString();
            string path = Path.Combine(Environment.CurrentDirectory, @"Data", batchId);

            List<BatchFile> files = new();

            foreach (var filePath in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(filePath);
                files.Add(new BatchFile() { Filename = fileName, Links = new Links() { Get = new Link() { Href = "/batch/" + batchId + "/files/" + fileName } } });
            }

            List<Models.Response.Attribute> attributes = new();

            attributes.Add(new Models.Response.Attribute { Key = "Exchange Set Type", Value = GetExchangeSetType(batchId) });
            attributes.Add(new Models.Response.Attribute { Key = "Media Type", Value = GetMediaType(batchId) });
            attributes.Add(new Models.Response.Attribute { Key = "Product Type", Value = "AVCS" });
            attributes.Add(new Models.Response.Attribute { Key = "S63 Version", Value = "1.2" });
            attributes.Add(new Models.Response.Attribute { Key = "Week Number", Value = currentWeek.ToString() });
            attributes.Add(new Models.Response.Attribute { Key = "Year", Value = currentYear });
            attributes.Add(new Models.Response.Attribute { Key = "Year / Week", Value = currentYear + " / " + currentWeek.ToString() });

            return new BatchDetail
            {
                BatchId = batchId,
                Status = "Committed",
                BusinessUnit = (batchId.ToLower() == EnumHelper.GetEnumDescription(Batch.EssFullAvcsZipBatch) || batchId.ToLower() == EnumHelper.GetEnumDescription(Batch.EssUpdateZipBatch)) ? "AVCSCustomExchangeSets" : "AVCSData",
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Attributes = attributes,
                Files = files
            };
        }

        private string GetMediaType(string batchId)
        {
            if (batchId.ToLower() == EnumHelper.GetEnumDescription(Batch.PosFullAvcsIsoSha1Batch))
                return "DVD";
            else
                return "Zip";
        }

        private string GetExchangeSetType(string batchId)
        {
            if (batchId.ToLower() == EnumHelper.GetEnumDescription(Batch.PosFullAvcsIsoSha1Batch) ||
                batchId.ToLower() == EnumHelper.GetEnumDescription(Batch.PosFullAvcsZipBatch))
                return "Base";
            else
                return "Update";
        }

        public byte[]? GetFileData(string homeDirectoryPath, string batchId, string fileName)
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

        public bool AddFile(string batchId, string fileName, string homeDirectoryPath)
        {
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            if (FileHelper.CheckFolderExists(batchFolderPath))
            {
                string srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, fileName);
                string destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchId), fileName);
                File.Copy(srcFile, destFile, true);
                return true;
            }
            return false;
        }

        public BatchStatusResponse GetBatchStatus(string batchId, string homeDirectoryPath)
        {
            BatchStatusResponse batchStatusResponse = new();
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
