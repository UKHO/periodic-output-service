using System.Globalization;
using Microsoft.Extensions.Options;
using UKHO.FmEssFssMock.API.Common;
using UKHO.FmEssFssMock.API.Helpers;
using UKHO.FmEssFssMock.API.Models.Response;
using UKHO.FmEssFssMock.Enums;

namespace UKHO.FmEssFssMock.API.Services
{
    public class FileShareService
    {
        private readonly IOptions<FileShareServiceConfiguration> fssConfiguration;
        private readonly string aioInfoFilesBatchId = "649C902D-5282-4CCF-924A-2B548EF42179";
        private readonly Dictionary<string, string> mimeTypes = new()
        {
            { ".zip", "application/zip" },
            { ".xml", "text/xml" },
            { ".csv", "text/csv" },
            { ".iso", "application/x-raw-disk-image" },
            { ".sha1", "text/plain" },
            { ".txt", "text/plain" }
        };

        private readonly Enum[] bessBatchTypes = {
                                            Batch.BessBaseZipBatch,
                                            Batch.BessChangeZipBatch,
                                            Batch.BessUpdateZipBatch,
                                            Batch.BessEmptyBatch,
                                            Batch.BessNoneReadmeBatch
                                     };

        private readonly Enum[] aioBatchTypes = new Enum[]
                                     {
                                            Batch.AioBaseCDZipIsoSha1Batch,
                                            Batch.AioUpdateZipBatch,
                                            Batch.EssAioBaseZipBatch,
                                            Batch.EssAioUpdateZipBatch
                                     };

        private readonly string DEFAULTMIMETYPE = "application/octet-stream";
        private readonly string bessSingleReadmeFileBatchId = "AB4A692D-6E3B-48A3-BD37-D232C60DD75D";
        private readonly string bessMultipleFilesBatchId = "10D40DD5-DDFB-497A-BB67-D99FB1658320";
        private const string BESPOKEREADME = "BESPOKE README";
        private const string MULTIPLEFILES = "MULTIPLE";
        private const string PERMITTXTFILENAME = "Permit.txt";
        private const string PERMITXMLFILENAME = "Permit.xml";

        public FileShareService(IOptions<FileShareServiceConfiguration> fssConfig)
        {
            fssConfiguration = fssConfig;
        }

        public BatchResponse CreateBatch(IEnumerable<KeyValuePair<string, string>> attributes, string homeDirectoryPath)
        {
            string attributeValue = attributes.FirstOrDefault(a => a.Key.ToLower() == "batch type").Value.ToLower();
            Enum.TryParse(attributeValue, true, out Batch batchType);
            string batchId = EnumHelper.GetEnumDescription(batchType);

            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            FileHelper.CheckAndCreateFolder(batchFolderPath);
            return new BatchResponse() { BatchId = Guid.Parse(batchId) };
        }

        private (string CurrentWeek, string CurrentYear) GetWeekNumber(bool isAioBatchType)
        {
            var now = isAioBatchType ? DateTime.UtcNow.AddDays(fssConfiguration.Value.WeeksToIncrement * 7) : DateTime.UtcNow;
            var currentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(now, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
            var currentYear = now.Year;

            if (currentWeek > 5 && now.Month < 2)
            {
                currentYear--;
            }

            return (currentWeek.ToString("00"), currentYear.ToString("0000"));
        }

        public BatchDetail GetBatchDetails(string batchId, string homeDirectoryPath)
        {
            batchId = batchId.ToLower();
            var isAioBatchType = aioBatchTypes.Contains(EnumHelper.GetValueFromDescription<Batch>(batchId));
            (var currentWeek, var currentYear) = GetWeekNumber(isAioBatchType);
            var path = Path.Combine(homeDirectoryPath, batchId);
            var businessUnit = "AVCSData";
            List<BatchFile> files = [];

            foreach (var filePath in Directory.GetFiles(path))
            {
                var fileInfo = new FileInfo(filePath);

                files.Add(new BatchFile
                {
                    Attributes = new List<Models.Response.Attribute>
                    {
                        new() { Key = "Product Type", Value = isAioBatchType ? "AIO" : "AVCS" },
                        new() { Key = "File Name", Value = fileInfo.Name }
                    },
                    MimeType = mimeTypes.ContainsKey(fileInfo.Extension.ToLower()) ? mimeTypes[fileInfo.Extension.ToLower()] : DEFAULTMIMETYPE,
                    FileSize = fileInfo.Length,
                    Hash = FileHelper.GetFileMD5(fileInfo),
                    Filename = fileInfo.Name,
                    Links = new Links()
                    {
                        Get = new Link()
                        {
                            Href = "/batch/" + batchId + "/files/" + fileInfo.Name
                        }
                    }
                });
            }

            //BESS - batch attributes from Queue message - start
            Batch batchEnu = EnumHelper.GetValueFromDescription<Batch>(batchId);
            if (bessBatchTypes.Contains(EnumHelper.GetValueFromDescription<Batch>(batchId)))
            {
                ConfigQueueMessage message = new()
                {
                    AllowedUserGroups = new List<string> { "FSS/BESS-Port of Atlantis" },
                    AllowedUsers = new List<string> { "FSS/BESS-Port of Atlantis" },
                    BatchExpiryInDays = 7,
                    Tags = new List<Tag> {
                            new() { Key = "Audience", Value = "Port of Atlantis" },
                            new() { Key = "Frequency",Value="Weekly" },
                            new() { Key = "Media Type",Value="Zip" },
                            new() { Key = "Product Type",Value="AVCS" },
                            new() { Key = "Year", Value = currentYear },
                            new() { Key = "Week Number", Value = currentWeek },
                            new() { Key = "Year / Week", Value = currentYear + " / " + currentWeek },
                }
                };

                List<KeyValuePair<string, string>> batchAttributes = new();

                foreach (Tag tag in message.Tags)
                {
                    batchAttributes.Add(new KeyValuePair<string, string>(tag.Key, tag.Value));
                }

                return new BatchDetail
                {
                    BatchId = batchId,
                    Status = GetBatchStatus(path),
                    BusinessUnit = "AVCSCustomExchangeSets",
                    ExpiryDate = DateTime.UtcNow.AddDays(message.BatchExpiryInDays).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    Attributes = batchAttributes,
                    Files = files
                };
            }

            //BESS - batch attributes from Queue message - end

            List<KeyValuePair<string, string>> attributes = new()
            {
                new("Product Type", isAioBatchType ? "AIO" : "AVCS"),
                new("Week Number", currentWeek),
                new("Year", currentYear),
                new("Year / Week", currentYear + " / " + currentWeek), };

            switch (EnumHelper.GetValueFromDescription<Batch>(batchId))
            {
                case Batch.PosFullAvcsIsoSha1Batch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "DVD"));
                    attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosFullAvcsZipBatch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Base"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosUpdateBatch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Update"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    attributes.Add(new KeyValuePair<string, string>("S63 Version", "1.2"));
                    break;

                case Batch.PosCatalogueBatch:
                    attributes.Add(new KeyValuePair<string, string>("Content", "Catalogue"));
                    attributes.Add(new KeyValuePair<string, string>("Catalogue Type", "XML"));
                    break;

                case Batch.PosEncUpdateBatch:
                    attributes.Add(new KeyValuePair<string, string>("Content", "ENC Updates"));
                    break;

                case Batch.AioBaseCDZipIsoSha1Batch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "AIO"));
                    break;

                case Batch.AioUpdateZipBatch:
                    attributes.Add(new KeyValuePair<string, string>("Exchange Set Type", "Update"));
                    attributes.Add(new KeyValuePair<string, string>("Media Type", "Zip"));
                    break;

                default:
                    businessUnit = "AVCSCustomExchangeSets";
                    break;
            };

            return new BatchDetail
            {
                BatchId = batchId,
                Status = GetBatchStatus(path),
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
                string srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, RenameFiles(fileName));
                string destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchId), fileName);
                File.Copy(srcFile, destFile, true);

                string permitXmlFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, PERMITXMLFILENAME);
                string permitTxtFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, PERMITTXTFILENAME);
                if (File.Exists(permitXmlFile))
                {
                    srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, RenameFiles(PERMITXMLFILENAME));
                    destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchId), PERMITXMLFILENAME);
                    File.Copy(srcFile, destFile, true);
                }
                if (File.Exists(permitTxtFile))
                {
                    srcFile = Path.Combine(Environment.CurrentDirectory, @"Data", batchId, RenameFiles(PERMITTXTFILENAME));
                    destFile = Path.Combine(Path.Combine(homeDirectoryPath, batchId), PERMITTXTFILENAME);
                    File.Copy(srcFile, destFile, true);
                }
                return true;
            }
            return false;
        }

        private string RenameFiles(string fileName)
        {
            return fileName.IndexOf("WK") > -1 ? fileName.Replace(fileName.Substring(fileName.IndexOf("WK"), 7), "WK34_22") : fileName;
        }

        public BatchStatusResponse GetBatchStatus(string batchId, string homeDirectoryPath)
        {
            BatchStatusResponse batchStatusResponse = new();
            string batchFolderPath = Path.Combine(homeDirectoryPath, batchId);

            if (FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckFolderExists(batchFolderPath))
            {
                if (FileHelper.ValidateFilePath(batchFolderPath) && FileHelper.CheckFolderExists(batchFolderPath))
                {
                    batchStatusResponse.BatchId = batchId;
                    batchStatusResponse.Status = GetBatchStatus(batchFolderPath);
                }
            }
            return batchStatusResponse;
        }

        private static string GetBatchStatus(string path) => File.Exists(Path.Combine(path, "CommitInProgress.txt")) ? "CommitInProgress" : "Committed";

        public SearchBatchResponse GetBatchResponse(string filter, string homeDirectoryPath)
        {
            if (filter.ToUpper().Contains("AIO CD INFO"))
            {
                return GetSearchBatchResponse(homeDirectoryPath, fssConfiguration.Value.FssInfoResponseFileName, aioInfoFilesBatchId);
            }
            else if (filter.ToUpper().Contains(BESPOKEREADME))
            {
                return GetSearchBatchResponse(homeDirectoryPath, fssConfiguration.Value.FssSingleReadMeResponseFileName, bessSingleReadmeFileBatchId);
            }
            else if (filter.ToUpper().Contains(MULTIPLEFILES))
            {
                return GetSearchBatchResponse(homeDirectoryPath, fssConfiguration.Value.FssMultipleReadMeResponseFileName, bessMultipleFilesBatchId);
            }

            return new SearchBatchResponse()
            {
                Entries = new List<BatchDetail>(),
                _Links = new PagingLinks()
                {
                    Self = new Link()
                    {
                        Href = "/batch?limit=10&start=0&$filter=%24batch%28Content%29%20eq%20%27AIO%20CD%20INFO%27%20and%20%24batch%28Product%20Type%29%20eq%20%27%27"
                    },
                }
            };
        }

        private SearchBatchResponse GetSearchBatchResponse(string homeDirectoryPath, string responseFileName, string batchId)
        {
            string responseFilePath = Path.Combine(fssConfiguration.Value.FssDataDirectoryPath, responseFileName);
            FileHelper.CheckAndCreateFolder(Path.Combine(homeDirectoryPath, batchId));

            string path = Path.Combine(Environment.CurrentDirectory, @"Data", batchId);
            foreach (string filePath in Directory.GetFiles(path))
            {
                FileInfo file = new(filePath);

                bool isFileAdded = AddFile(batchId, file.Name, homeDirectoryPath);

                if (!isFileAdded)
                {
                    return null;
                }
            }
            return FileHelper.ReadJsonFile<SearchBatchResponse>(responseFilePath);
        }
    }
}
