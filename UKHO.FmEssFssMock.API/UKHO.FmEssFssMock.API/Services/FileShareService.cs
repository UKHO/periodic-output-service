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
            string attributeValue = attributes.FirstOrDefault(a => a.Key.ToLower() == "batch type").Value.ToLower();
            Enum.TryParse(attributeValue, true, out Batch batchType);
            string batchId = EnumHelper.GetEnumDescription(batchType);

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
            string businessUnit = "AVCSData";

            List<BatchFile> files = new();

            foreach (string? filePath in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(filePath);
                files.Add(new BatchFile() { Filename = fileName, Links = new Links() { Get = new Link() { Href = "/batch/" + batchId + "/files/" + fileName } } });
            }

            List<KeyValuePair<string, string>> attributes = new()
            {
                new KeyValuePair<string, string>("Product Type", "AVCS"),
                new KeyValuePair<string, string>("S63 Version", "1.2"),
                new KeyValuePair<string, string>("Week Number", currentWeek.ToString()),
                new KeyValuePair<string, string>("Year", currentYear),
                new KeyValuePair<string, string>("Year / Week", currentYear + " / " + currentWeek.ToString()),
            };

            switch (EnumHelper.GetValueFromDescription<Batch>(batchId))
            {
                case Batch.PosFullAvcsIsoSha1Batch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "DVD"));
                    break;

                case Batch.PosFullAvcsZipBatch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    break;

                case Batch.PosUpdateBatch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Update"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    break;

                case Batch.PosCatalogueBatch:
                    attributes.Add(new KeyValuePair<string, string>("Content", "Catalogue"));
                    attributes.Add(new KeyValuePair<string, string>("Catalogue Type", "XML"));
                    break;

                case Batch.PosEncUpdateBatch:
                    attributes.Add(new KeyValuePair<string, string>("Content", "ENC Updates"));
                    break;

                default:
                    businessUnit = "AVCSCustomExchangeSets";
                    break;
            };

            return new BatchDetail
            {
                BatchId = batchId,
                Status = "Committed",
                BusinessUnit = businessUnit,
                ExpiryDate = DateTime.UtcNow.AddDays(28).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                Attributes = attributes,
                Files = files
            };
        }

        public byte[]? GetFileData(string homeDirectoryPath, string batchId, string fileName)
        {
            byte[] bytes = null;
            string? filePath = Path.Combine(homeDirectoryPath, batchId, fileName);

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
            foreach (string item in batchId)
            {
                string exchangeSetZipFolderPath = Path.Combine(homeDirectoryPath, item);
                bool response = FileHelper.CleanUp(exchangeSetZipFolderPath);
                if (response)
                {
                    deleteFlag = true;
                }
            }
            return deleteFlag;
        }
    }
}
