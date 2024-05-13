using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Abstractions;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Extensions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Ess;
using UKHO.PeriodicOutputService.Common.Models.Ess.Response;
using UKHO.PeriodicOutputService.Common.Models.Fss;
using UKHO.PeriodicOutputService.Common.Models.Fss.Response;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.PermitDecryption;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.BuilderService.Services
{
    public class BuilderService : IBuilderService
    {
        private readonly IEssService essService;
        private readonly IFssService fssService;
        private readonly IConfiguration configuration;
        private readonly IFileSystemHelper fileSystemHelper;
        private readonly ILogger<BuilderService> logger;
        private readonly IAzureTableStorageHelper azureTableStorageHelper;
        private readonly IOptions<FssApiConfiguration> fssApiConfig;
        private readonly IPksService pksService;
        private readonly IPermitDecryption permitDecryption;

        private readonly string homeDirectoryPath;
        private readonly Dictionary<string, string> mimeTypes = new()
        {
            { ".zip", "application/zip" },
            { ".xml", "text/xml" },
            { ".csv", "text/csv" },
            { ".txt", "text/plain" }
        };

        private const string BESSBATCHFILEEXTENSION = "zip;xml;txt;csv";
        private const string PERMITTEXTFILE = "Permit.txt";
        private const string PERMITXMLFILE = "Permit.xml";
        private const string PERMITTEXTFILEHEADER = "Key ID,Key,Name,Edition,Created,Issued,Expired,Status";        
        private const string DEFAULTMIMETYPE = "application/octet-stream";

        

        public BuilderService(IEssService essService, IFssService fssService, IConfiguration configuration, IFileSystemHelper fileSystemHelper, ILogger<BuilderService> logger, IAzureTableStorageHelper azureTableStorageHelper, IOptions<FssApiConfiguration> fssApiConfig, IPksService pksService, IPermitDecryption permitDecryption)
        {
            this.essService = essService ?? throw new ArgumentNullException(nameof(essService));
            this.fssService = fssService ?? throw new ArgumentNullException(nameof(fssService));
            this.configuration = configuration;
            this.fileSystemHelper = fileSystemHelper ?? throw new ArgumentNullException(nameof(fileSystemHelper));
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.azureTableStorageHelper = azureTableStorageHelper ?? throw new ArgumentNullException(nameof(azureTableStorageHelper));
            this.fssApiConfig = fssApiConfig ?? throw new ArgumentNullException(nameof(fssApiConfig));
            this.pksService = pksService ?? throw new ArgumentNullException(nameof(pksService));
            this.permitDecryption = permitDecryption ?? throw new ArgumentNullException(nameof(permitDecryption));

            homeDirectoryPath = Path.Combine(configuration["HOME"]!, configuration["BespokeFolderName"]!);
        }

        public async Task<bool> CreateBespokeExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            string essBatchId = await RequestExchangeSetAsync(configQueueMessage);

            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSetAsync(essBatchId);

            ExtractExchangeSetZip(essFiles, essFileDownloadPath);

            await PerformAncillaryFilesOperationsAsync(essBatchId, configQueueMessage, essFileDownloadPath);

            ProductVersionsRequest? latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, configQueueMessage.EncCellNames.ToArray());

            if (Enum.TryParse(configQueueMessage.KeyFileType, false, out KeyFileType fileType) && !string.Equals(configQueueMessage.KeyFileType, KeyFileType.NONE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                List<ProductKeyServiceRequest> productKeyServiceRequest = new();

                productKeyServiceRequest.AddRange(latestProductVersions.ProductVersions.Select(
                    item => new ProductKeyServiceRequest()
                    {
                        ProductName = item.ProductName,
                        Edition = item.EditionNumber.ToString()
                    }));

                List<ProductKeyServiceResponse> productKeyServiceResponse = await pksService.PostProductKeyData(productKeyServiceRequest);

                CreatePermitFile(fileType, essFileDownloadPath, productKeyServiceResponse);
            }

            CreateZipFile(essFiles, essFileDownloadPath);

            if (bool.Parse(configuration["IsFTRunning"]))
            {
                await IsBatchCreatedForMock(configQueueMessage, essFileDownloadPath);
            }
            if (!CreateBessBatchAsync(essFileDownloadPath, BESSBATCHFILEEXTENSION, configQueueMessage).Result)
                return false;

            if (configQueueMessage.Type == BessType.UPDATE.ToString() ||
                     configQueueMessage.Type == BessType.CHANGE.ToString())
            {
                if (latestProductVersions.ProductVersions.Count > 0)
                {
                    LogProductVersions(latestProductVersions, configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);
                }
                else
                {
                    logger.LogInformation(EventIds.EmptyBatchResponse.ToEventId(), "Latest edition/update details not found. | DateTime: {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
            }

            return true;
        }

        private async Task<string> RequestExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            ExchangeSetResponseModel exchangeSetResponseModel = new();
            if (configQueueMessage.Type == BessType.BASE.ToString())
            {
                exchangeSetResponseModel = await essService.PostProductIdentifiersData((List<string>)configQueueMessage.EncCellNames, configQueueMessage.ExchangeSetStandard);
            }
            else if (configQueueMessage.Type == BessType.UPDATE.ToString() ||
                     configQueueMessage.Type == BessType.CHANGE.ToString())
            {
                var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

                var productVersions = GetProductVersionsFromEntities(productVersionEntities, configQueueMessage.EncCellNames.ToArray(), configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

                exchangeSetResponseModel = await essService.GetProductDataProductVersions(new ProductVersionsRequest()
                {
                    ProductVersions = productVersions
                }, configQueueMessage.ExchangeSetStandard);
            }

            logger.LogInformation(EventIds.ProductsFetchedFromESS.ToEventId(),
                "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}",
                configQueueMessage.EncCellNames.Count(), exchangeSetResponseModel.ExchangeSetCellCount, exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

            return CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
        }

        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSetAsync(string essBatchId)
        {
            string downloadPath = Path.Combine(homeDirectoryPath, essBatchId);
            List<FssBatchFile> files;

            FssBatchStatus fssBatchStatus = await fssService.CheckIfBatchCommitted(essBatchId, RequestType.BESS);

            if (fssBatchStatus == FssBatchStatus.Committed)
            {
                fileSystemHelper.CreateDirectory(downloadPath);
                files = await GetBatchFilesAsync(essBatchId);
                DownloadFiles(files, downloadPath);
            }
            else
            {
                logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), fssBatchStatus, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
            }

            return (downloadPath, files);
        }

        private async Task<List<FssBatchFile>> GetBatchFilesAsync(string essBatchId)
        {
            GetBatchResponseModel batchDetail = await fssService.GetBatchDetails(essBatchId);
            List<FssBatchFile> batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href, FileSize = a.FileSize }).ToList();

            if (batchFiles.Any() && !batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
            {
                return batchFiles;
            }

            logger.LogError(EventIds.ErrorFileFoundInBatch.ToEventId(), "Either no files found in batch or error file found in batch with BatchID - {BatchID} | {DateTime} | _X-Correlation-ID:{CorrelationId}", essBatchId, DateTime.UtcNow, CommonHelper.CorrelationID);
            throw new FulfilmentException(EventIds.ErrorFileFoundInBatch.ToEventId());
        }

        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                fssService.DownloadFileAsync(file.FileName, file.FileLink, file.FileSize, filePath).Wait();
            });
        }

        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ExtractZipFileStarted.ToEventId(), "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);

                    logger.LogInformation(EventIds.ExtractZipFileCompleted.ToEventId(), "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ExtractZipFileFailed.ToEventId(), "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Extracting zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private void CreateZipFile(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ZipFileCreationStarted.ToEventId(), "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                    fileSystemHelper.CreateZipFile(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), Path.Combine(downloadPath, file.FileName), true);

                    logger.LogInformation(EventIds.ZipFileCreationCompleted.ToEventId(), "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ZipFileCreationFailed.ToEventId(), "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}", file.FileName, DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Creating zip file {file.FileName} failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        private async Task<bool> CreateBessBatchAsync(string downloadPath, string fileExtension, ConfigQueueMessage configQueueMessage)
        {
            bool isCommitted;
            string bessBatchId;
            Batch batchType = Batch.BessBaseZipBatch;

            try
            {
                logger.LogInformation(EventIds.BessBatchCreationStarted.ToEventId(), "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                if (configQueueMessage.Type.ToUpper().Equals(BessType.UPDATE.ToString()))
                {
                    batchType = Batch.BessUpdateZipBatch;
                }
                else if (configQueueMessage.Type.ToUpper().Equals(BessType.CHANGE.ToString()))
                {
                    batchType = Batch.BessChangeZipBatch;
                }
                //else if block for mock only
                else if (configQueueMessage.Type.ToUpper().Equals("EMPTY"))
                {
                    batchType = Batch.BessEmptyBatch;
                }

                bessBatchId = await fssService.CreateBatch(batchType, configQueueMessage);

                IEnumerable<string> filePath = fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);

                UploadBatchFiles(filePath, bessBatchId, batchType);

                isCommitted = await fssService.CommitBatch(bessBatchId, filePath, batchType);

                logger.LogInformation(EventIds.BessBatchCreationCompleted.ToEventId(), "BESS batch {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}", bessBatchId, isCommitted, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessBatchCreationFailed.ToEventId(), "BESS batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}", ex, DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                throw new FulfilmentException(EventIds.BessBatchCreationFailed.ToEventId());
            }

            return isCommitted;
        }

        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId, Batch batchType)
        {
            Parallel.ForEach(filePaths, filePath =>
            {
                IFileInfo fileInfo = fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length, mimeTypes.ContainsKey(fileInfo.Extension.ToLower()) ? mimeTypes[fileInfo.Extension.ToLower()] : DEFAULTMIMETYPE, batchType).Result;
                if (isFileAdded)
                {
                    List<string> blockIds = fssService.UploadBlocks(batchId, fileInfo).Result;
                    if (blockIds.Any())
                    {
                        bool fileWritten = fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds).Result;
                    }
                }
            });
        }
        [ExcludeFromCodeCoverage]
        private List<ProductVersion> GetProductVersionsFromEntities(List<ProductVersionEntities> productVersionEntities, string[] cellNames, string configName, string exchangeSetStandard)
        {
            List<ProductVersion> productVersions = new();

            foreach (var cellName in cellNames)
            {
                ProductVersion productVersion = new();

                var result = productVersionEntities.Where(p => p.PartitionKey == configName && p.RowKey == exchangeSetStandard + "|" + cellName);

                if (result.Any())
                {
                    productVersion.ProductName = result.FirstOrDefault().RowKey.Split("|")[1];
                    productVersion.EditionNumber = result.FirstOrDefault().EditionNumber;
                    productVersion.UpdateNumber = result.FirstOrDefault().UpdateNumber;
                }
                else
                {
                    productVersion.ProductName = cellName;
                    productVersion.EditionNumber = 0;
                    productVersion.UpdateNumber = 0;
                }
                productVersions.Add(productVersion);
            }

            return productVersions;
        }

        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] cellNames)
        {
            string exchangeSetPath = Path.Combine(filePath, fssApiConfig.Value.BespokeExchangeSetFileFolder);

            ProductVersionsRequest productVersionsRequest = new()
            {
                ProductVersions = new()
            };

            foreach (var cellName in cellNames)
            {
                var productVersions = fileSystemHelper.GetProductVersionsFromDirectory(exchangeSetPath, cellName);

                productVersionsRequest.ProductVersions.AddRange(productVersions);
            }
            return productVersionsRequest;
        }

        private void LogProductVersions(ProductVersionsRequest productVersionsRequest, string name, string exchangeSetStandard)
        {
            try
            {
                logger.LogInformation(EventIds.LoggingProductVersionsStarted.ToEventId(), "Logging product version started | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                azureTableStorageHelper.SaveBessProductVersionDetailsAsync(productVersionsRequest.ProductVersions, name, exchangeSetStandard);

                logger.LogInformation(EventIds.LoggingProductVersionsCompleted.ToEventId(), "Logging product version completed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.LoggingProductVersionsFailed.ToEventId(), "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.Now.ToUniversalTime(), CommonHelper.CorrelationID);

                throw new Exception($"Logging Product version failed at {DateTime.Now.ToUniversalTime()} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
            }
        }

        private async Task PerformAncillaryFilesOperationsAsync(string batchId, ConfigQueueMessage configQueueMessage, string essFileDownloadPath)
        {
            string exchangeSetPath = Path.Combine(homeDirectoryPath, batchId, fssApiConfig.Value.BespokeExchangeSetFileFolder);
            string exchangeSetRootPath = Path.Combine(exchangeSetPath, fssApiConfig.Value.EncRoot);
            string readMeFilePath = Path.Combine(exchangeSetRootPath, fssApiConfig.Value.ReadMeFileName);
            await CreateReadMeFileAsync(batchId, configQueueMessage.CorrelationId, configQueueMessage.ReadMeSearchFilter, exchangeSetRootPath, readMeFilePath);

            string exchangeSetInfoPath = Path.Combine(essFileDownloadPath, fssApiConfig.Value.BespokeExchangeSetFileFolder, fssApiConfig.Value.Info);
            string serialFilePath = Path.Combine(essFileDownloadPath, fssApiConfig.Value.BespokeExchangeSetFileFolder, fssApiConfig.Value.SerialFileName);
            string productFilePath = Path.Combine(essFileDownloadPath, fssApiConfig.Value.BespokeExchangeSetFileFolder, fssApiConfig.Value.Info, fssApiConfig.Value.ProductFileName);

            await UpdateSerialFileAsync(serialFilePath, configQueueMessage.Type, configQueueMessage.CorrelationId);

            await DeleteProductTxtAndInfoFolderAsync(productFilePath, exchangeSetInfoPath, configQueueMessage.CorrelationId);
        }

        private async Task CreateReadMeFileAsync(string batchId, string correlationId, string readMeSearchFilter, string exchangeSetRootPath, string readMeFilePath)
        {
            if (readMeSearchFilter == ReadMeSearchFilter.AVCS.ToString())
            {
                return;
            }

            if (readMeSearchFilter == ReadMeSearchFilter.BLANK.ToString())
            {
                fileSystemHelper.CreateEmptyFileContent(readMeFilePath);
            }
            else
            {
                await DownloadReadMeFileAsync(batchId, exchangeSetRootPath, correlationId, readMeSearchFilter);
            }
        }

        private async Task<bool> DownloadReadMeFileAsync(string batchId, string exchangeSetRootPath, string correlationId, string readMeSearchFilter)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                  EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                  "File share service search query request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                  async () => await fssService.SearchReadMeFilePathAsync(batchId, correlationId, readMeSearchFilter),
               batchId, correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme.txt file for BatchId:{BatchId} and _X-Correlation-ID:{CorrelationId}",
                   async () => await fssService.DownloadReadMeFileAsync(readMeFilePath, batchId, exchangeSetRootPath, correlationId),
                batchId, correlationId);
            }

            return isDownloadReadMeFileSuccess;
        }

        private async Task UpdateSerialFileAsync(string serialFilePath, string exchangeSetType, string correlationId)
        {
            try
            {
                string serialFileContent = fileSystemHelper.ReadFileText(serialFilePath);
                const string searchText = "UPDATE";

                if (serialFileContent.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) > -1)
                {
                    serialFileContent = Regex.Replace(serialFileContent, searchText, exchangeSetType, RegexOptions.IgnoreCase);

                    fileSystemHelper.CreateFileContent(serialFilePath, serialFileContent);

                    logger.LogInformation(EventIds.BessSerialEncUpdated.ToEventId(), "SERIAL.ENC file updated with Type: {exchangeSetType} | _X-Correlation-ID:{CorrelationId}", exchangeSetType, correlationId);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessSerialEncUpdateFailed.ToEventId(), "SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.BessSerialEncUpdateFailed.ToEventId());
            }

            await Task.CompletedTask;
        }

        private async Task DeleteProductTxtAndInfoFolderAsync(string productFilePath, string infoFolderPath, string correlationId)
        {
            try
            {
                fileSystemHelper.DeleteFile(productFilePath);

                fileSystemHelper.DeleteFolder(infoFolderPath);

                logger.LogInformation(EventIds.BessProductTxtAndInfoFolderDeleted.ToEventId(), "PRODUCT.TXT file and INFO folder deleted | _X-Correlation-ID:{CorrelationId}", correlationId);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId(), "PRODUCT.TXT file and INFO folder delete operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", DateTime.Now.ToUniversalTime(), ex.Message, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId());
            }

            await Task.CompletedTask;
        }

        // This method is for mock only
        [ExcludeFromCodeCoverage]
        private async Task<bool> IsBatchCreatedForMock(ConfigQueueMessage configQueueMessage, string essFileDownloadPath)
        {
            bool isBatchCreated;            
            var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

                var productVersions = GetProductVersionsFromEntities(productVersionEntities, configQueueMessage.EncCellNames.ToArray(),
                configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

            var product = productVersions.Any(x => x.EditionNumber > 0);
            if (product)
            {
                configQueueMessage.Type = "EMPTY";
            }
            isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BESSBATCHFILEEXTENSION, configQueueMessage).Result;

            return isBatchCreated;
        }

        private void CreatePermitFile(KeyFileType keyFileType, string filePath, List<ProductKeyServiceResponse> productKeyServiceResponses)
        {
            logger.LogInformation(EventIds.PermitFileCreationStarted.ToEventId(), "Permit file creation started for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}", keyFileType, DateTime.UtcNow, CommonHelper.CorrelationID);

            if (keyFileType == KeyFileType.KEY_TEXT)
            {
                int rowNumber = 1;
                string permitFileContent = PERMITTEXTFILEHEADER;

                foreach (var productKeyServiceResponse in productKeyServiceResponses)
                {
                    PermitKey permitKey = permitDecryption.GetPermitKeys(productKeyServiceResponse.Key);

                    if (permitKey != null)
                    {
                        string date = DateTime.UtcNow.ToString("dd/MM/yyyy", CultureInfo.InvariantCulture);

                        permitFileContent += Environment.NewLine;
                        permitFileContent += $"{rowNumber++},{permitKey.ActiveKey},{productKeyServiceResponse.ProductName},{productKeyServiceResponse.Edition},{date},{date},,1:Active";
                        permitFileContent += Environment.NewLine;
                        permitFileContent += $"{rowNumber++},{permitKey.NextKey},{productKeyServiceResponse.ProductName},{Convert.ToInt16(productKeyServiceResponse.Edition) + 1},{date},{date},,2:Next";
                    }
                };

                fileSystemHelper.CreateTextFile(filePath, PERMITTEXTFILE, permitFileContent);
            }
            else if (keyFileType == KeyFileType.PERMIT_XML)
            {
                PksXml pKSXml = new()
                {
                    Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                    Cellkeys = new()
                    {
                        ProductKeyServiceResponses = productKeyServiceResponses,
                    }
                };

                fileSystemHelper.CreateXmlFromObject(pKSXml, filePath, PERMITXMLFILE);
            }

            logger.LogInformation(EventIds.PermitFileCreationCompleted.ToEventId(), "Permit file creation completed for {KeyFileType} | {DateTime} | _X-Correlation-ID : {CorrelationId}", keyFileType, DateTime.UtcNow, CommonHelper.CorrelationID);
        }
    }
}
