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

        private const string BESSBATCHFILEEXTENSION = "zip;xml;txt;csv";
        private const string PERMITTEXTFILE = "Permit.txt";
        private const string PERMITXMLFILE = "Permit.xml";
        private const string PERMITTEXTFILEHEADER = "Key ID,Key,Name,Edition,Created,Issued,Expired,Status";
        private const string DEFAULTMIMETYPE = "application/octet-stream";
        private const string BESSFOLDERNAME = "BESSFolderName";
        private const string HOME = "HOME";

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

            homeDirectoryPath = Path.Combine(configuration[HOME]!, configuration[BESSFOLDERNAME]!);
        }

        /// <summary>
        ///     This method will request exchange set to ESS,
        ///     download batch files from FSS,
        ///     create BESS and upload to FSS.
        /// </summary>
        /// <param name="configQueueMessage"></param>
        /// <returns>Returns true/false</returns>
        public async Task<bool> CreateBespokeExchangeSetAsync(ConfigQueueMessage configQueueMessage)
        {
            string essBatchId = await RequestExchangeSetAsync(configQueueMessage);

            (string essFileDownloadPath, List<FssBatchFile> essFiles) = await DownloadEssExchangeSetAsync(essBatchId);

            string bessZipFileName = string.Format(fssApiConfig.Value.BESSZipFileName, configQueueMessage.Name);

            RenameFile(essFileDownloadPath, essFiles, bessZipFileName);

            ExtractExchangeSetZip(essFiles, essFileDownloadPath);

            await PerformAncillaryFilesOperationsAsync(essBatchId, configQueueMessage, essFileDownloadPath, bessZipFileName);

            var latestProductVersions = GetTheLatestUpdateNumber(essFileDownloadPath, configQueueMessage.EncCellNames.ToArray(), bessZipFileName);

            await RequestCellKeysFromPksAsync(configQueueMessage, essFileDownloadPath, latestProductVersions);

            CreateZipFile(essFiles, essFileDownloadPath);

            if (bool.Parse(configuration["IsFTRunning"]))
            {
                await IsBatchCreatedForMock(configQueueMessage, essFileDownloadPath);
            }

            if (!CreateBessBatchAsync(essFileDownloadPath, BESSBATCHFILEEXTENSION, configQueueMessage).Result)
            {
                return false;
            }

            await SaveLatestProductVersionDetailsAsync(configQueueMessage, latestProductVersions);

            return true;
        }

        /// <summary>
        /// This method will add/update entry of the product version details in azure table
        /// </summary>
        /// <param name="configQueueMessage"></param>
        /// <param name="latestProductVersions"></param>
        /// <returns></returns>
        private async Task SaveLatestProductVersionDetailsAsync(ConfigQueueMessage configQueueMessage, ProductVersionsRequest latestProductVersions)
        {
            if (configQueueMessage.Type == BessType.UPDATE.ToString() ||
                                 configQueueMessage.Type == BessType.CHANGE.ToString())
            {
                if (latestProductVersions.ProductVersions.Count > 0)
                {
                    await LogProductVersionsAsync(latestProductVersions, configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);
                }
                else
                {
                    logger.LogInformation(EventIds.EmptyBatchResponse.ToEventId(), "Latest edition/update details not found. | DateTime: {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
                }
            }
        }

        /// <summary>
        /// This method will request CellKeys from ProductKeyService.
        /// </summary>
        /// <param name="configQueueMessage"></param>
        /// <param name="essFileDownloadPath"></param>
        /// <param name="latestProductVersions"></param>
        /// <returns></returns>
        private async Task RequestCellKeysFromPksAsync(ConfigQueueMessage configQueueMessage, string essFileDownloadPath, ProductVersionsRequest latestProductVersions)
        {
            if (Enum.TryParse(configQueueMessage.KeyFileType, false, out KeyFileType fileType) && !string.Equals(configQueueMessage.KeyFileType, KeyFileType.NONE.ToString(), StringComparison.OrdinalIgnoreCase))
            {
                List<ProductKeyServiceRequest> productKeyServiceRequest = ProductKeyServiceRequest(latestProductVersions);

                List<ProductKeyServiceResponse> productKeyServiceResponse = await pksService.PostProductKeyData(productKeyServiceRequest);

                CreatePermitFile(fileType, essFileDownloadPath, productKeyServiceResponse);
            }
        }

        /// <summary>
        ///     This method will request exchange set to ESS.
        /// </summary>
        /// <param name="configQueueMessage"></param>
        /// <returns>will return batch id from ESS</returns>
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

                var productVersions = GetProductVersionsFromEntities(productVersionEntities, configQueueMessage.EncCellNames, configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

                exchangeSetResponseModel = await essService.GetProductDataProductVersions(new ProductVersionsRequest
                {
                    ProductVersions = productVersions
                }, configQueueMessage.ExchangeSetStandard);
            }

            logger.LogInformation(EventIds.ProductsFetchedFromESS.ToEventId(),
                "No of Products requested to ESS : {productCount}, No of valid Cells count received from ESS: {cellCount} and Invalid cells count: {invalidCellCount} | DateTime: {DateTime} | _X-Correlation-ID:{CorrelationId}",
                configQueueMessage.EncCellNames.Count(), exchangeSetResponseModel.ExchangeSetCellCount, exchangeSetResponseModel.RequestedProductsNotInExchangeSet.Count(), DateTime.UtcNow, CommonHelper.CorrelationID);

            return CommonHelper.ExtractBatchId(exchangeSetResponseModel.Links.ExchangeSetBatchDetailsUri.Href);
        }

        /// <summary>
        ///     This method will check if ESS batch is committed and then will download batch files from FSS.
        /// </summary>
        /// <param name="essBatchId"></param>
        /// <returns></returns>
        /// <exception cref="FulfilmentException"></exception>
        private async Task<(string, List<FssBatchFile>)> DownloadEssExchangeSetAsync(string essBatchId)
        {
            string downloadPath = Path.Combine(homeDirectoryPath, essBatchId);
            List<FssBatchFile> files;

            FssBatchStatus fssBatchStatus = await fssService.CheckIfBatchCommitted(essBatchId, RequestType.BESS);

            if (fssBatchStatus == FssBatchStatus.Committed)
            {
                files = await GetBatchFilesAsync(essBatchId);
                fileSystemHelper.CreateDirectory(downloadPath);
                DownloadFiles(files, downloadPath);
            }
            else
            {
                logger.LogError(EventIds.FssPollingCutOffTimeout.ToEventId(), "Batch is not committed within given polling cut off time | {DateTime} | Batch Status : {BatchStatus} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, fssBatchStatus, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.FssPollingCutOffTimeout.ToEventId());
            }

            return (downloadPath, files);
        }

        /// <summary>
        ///     This method will get batch files details from FSS.
        /// </summary>
        /// <param name="essBatchId"></param>
        /// <returns>List of FssBatchFile</returns>
        /// <exception cref="FulfilmentException"></exception>
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

        /// <summary>
        ///     This method will download batch files from FSS.
        /// </summary>
        /// <param name="fileDetails"></param>
        /// <param name="downloadPath"></param>
        private void DownloadFiles(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                string filePath = Path.Combine(downloadPath, file.FileName);
                fssService.DownloadFileAsync(file.FileName, file.FileLink, file.FileSize, filePath).Wait();
            });
        }

        /// <summary>
        ///     This method will extract files from ESS batch zip.
        /// </summary>
        /// <param name="fileDetails"></param>
        /// <param name="downloadPath"></param>
        /// <exception cref="Exception"></exception>
        private void ExtractExchangeSetZip(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ExtractZipFileStarted.ToEventId(), "Extracting zip file {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.UtcNow, CommonHelper.CorrelationID);

                    fileSystemHelper.ExtractZipFile(Path.Combine(downloadPath, file.FileName), Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), true);

                    logger.LogInformation(EventIds.ExtractZipFileCompleted.ToEventId(), "Extracting zip file {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.UtcNow, CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ExtractZipFileFailed.ToEventId(), "Extracting zip file {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Extracting zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        /// <summary>
        ///     This method will create BESS zip file.
        /// </summary>
        /// <param name="fileDetails"></param>
        /// <param name="downloadPath"></param>
        /// <exception cref="Exception"></exception>
        private void CreateZipFile(List<FssBatchFile> fileDetails, string downloadPath)
        {
            Parallel.ForEach(fileDetails, file =>
            {
                try
                {
                    logger.LogInformation(EventIds.ZipFileCreationStarted.ToEventId(), "Creating zip file of directory {fileName} started at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.UtcNow, CommonHelper.CorrelationID);

                    fileSystemHelper.CreateZipFile(Path.Combine(downloadPath, Path.GetFileNameWithoutExtension(file.FileName)), Path.Combine(downloadPath, file.FileName), true);

                    logger.LogInformation(EventIds.ZipFileCreationCompleted.ToEventId(), "Creating zip file of directory {fileName} completed at {DateTime} | _X-Correlation-ID:{CorrelationId}", file.FileName, DateTime.UtcNow, CommonHelper.CorrelationID);
                }
                catch (Exception ex)
                {
                    logger.LogError(EventIds.ZipFileCreationFailed.ToEventId(), "Creating zip file of directory {fileName} failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID: {CorrelationId}", file.FileName, DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                    throw new Exception($"Creating zip file {file.FileName} failed at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
                }
            });
        }

        /// <summary>
        ///     This method will create, upload and commit BESS batch to FSS.
        /// </summary>
        /// <param name="downloadPath"></param>
        /// <param name="fileExtension"></param>
        /// <param name="configQueueMessage"></param>
        /// <returns>Return true or false</returns>
        /// <exception cref="FulfilmentException"></exception>
        private async Task<bool> CreateBessBatchAsync(string downloadPath, string fileExtension, ConfigQueueMessage configQueueMessage)
        {
            bool isCommitted;
            Batch batchType = Batch.BessBaseZipBatch;

            try
            {
                logger.LogInformation(EventIds.BessBatchCreationStarted.ToEventId(), "BESS batch creation started at {DateTime} | _X-Correlation-ID: {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);

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

                string bessBatchId = await fssService.CreateBatch(batchType, configQueueMessage);

                var filePaths = fileSystemHelper.GetFiles(downloadPath, fileExtension, SearchOption.TopDirectoryOnly);

                UploadBatchFiles(filePaths, bessBatchId, batchType);

                isCommitted = await fssService.CommitBatch(bessBatchId, filePaths, batchType);

                logger.LogInformation(EventIds.BessBatchCreationCompleted.ToEventId(), "BESS batch {bessBatchId} is created and added to FSS with status: {isCommitted} at {DateTime} | _X-Correlation-ID: {CorrelationId}", bessBatchId, isCommitted, DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.BessBatchCreationFailed.ToEventId(), "BESS batch creation failed with Exception: {ex} at {DateTime} | _X-Correlation-ID : {CorrelationId}", ex, DateTime.UtcNow, CommonHelper.CorrelationID);

                throw new FulfilmentException(EventIds.BessBatchCreationFailed.ToEventId());
            }

            return isCommitted;
        }

        /// <summary>
        ///     This method will upload BESS batch file to FSS.
        /// </summary>
        /// <param name="filePaths"></param>
        /// <param name="batchId"></param>
        /// <param name="batchType"></param>
        private void UploadBatchFiles(IEnumerable<string> filePaths, string batchId, Batch batchType)
        {
            Parallel.ForEach(filePaths, filePath =>
            {
                IFileInfo fileInfo = fileSystemHelper.GetFileInfo(filePath);

                bool isFileAdded = fssService.AddFileToBatch(batchId, fileInfo.Name, fileInfo.Length, CommonHelper.MimeTypeList().ContainsKey(fileInfo.Extension.ToLower()) ? CommonHelper.MimeTypeList()[fileInfo.Extension.ToLower()] : DEFAULTMIMETYPE, batchType).Result;
                if (isFileAdded)
                {
                    List<string> blockIds = fssService.UploadBlocks(batchId, fileInfo).Result;
                    if (blockIds.Any())
                    {
                        fssService.WriteBlockFile(batchId, fileInfo.Name, blockIds);
                    }
                }
            });
        }

        /// <summary>
        ///     This method will return list of product versions from product version azure table.
        /// </summary>
        /// <param name="productVersionEntities"></param>
        /// <param name="cellNames"></param>
        /// <param name="configName"></param>
        /// <param name="exchangeSetStandard"></param>
        /// <returns></returns>
        [ExcludeFromCodeCoverage]
        private List<ProductVersion> GetProductVersionsFromEntities(List<ProductVersionEntities> productVersionEntities, IEnumerable<string> cellNames, string configName, string exchangeSetStandard)
        {
            List<ProductVersion> productVersions = new();

            foreach (var cellName in cellNames)
            {
                ProductVersion productVersion = new();

                var result = productVersionEntities.Where(p => p.PartitionKey == configName && p.RowKey == exchangeSetStandard + "|" + cellName).ToList();

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

        /// <summary>
        /// This method will return the latest edition and update number of products from directory.
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="cellNames"></param>
        /// <param name="bessZipFileName"></param>
        /// <returns></returns>
        private ProductVersionsRequest GetTheLatestUpdateNumber(string filePath, string[] cellNames, string bessZipFileName)
        {
            string exchangeSetPath = Path.Combine(filePath, bessZipFileName);

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

        /// <summary>
        ///     This method will add/update entry of the product version details in azure table.
        /// </summary>
        /// <param name="productVersionsRequest"></param>
        /// <param name="configName"></param>
        /// <param name="exchangeSetStandard"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        private async Task LogProductVersionsAsync(ProductVersionsRequest productVersionsRequest, string configName, string exchangeSetStandard)
        {
            try
            {
                await logger.LogStartEndAndElapsedTime(EventIds.LoggingProductVersionsStarted,
                    EventIds.LoggingProductVersionsCompleted,
                    "Logging product version details for Config Name:{configName} | {DateTime} | _X-Correlation-ID:{CorrelationId}",
                    async () => await azureTableStorageHelper.SaveBessProductVersionDetailsAsync(productVersionsRequest.ProductVersions, configName, exchangeSetStandard),
                    configName, DateTime.UtcNow, CommonHelper.CorrelationID);
            }
            catch (Exception ex)
            {
                logger.LogError(EventIds.LoggingProductVersionsFailed.ToEventId(), "Logging product version failed | {DateTime} | _X-Correlation-ID : {CorrelationId}", DateTime.UtcNow, CommonHelper.CorrelationID);
                throw new Exception($"Logging product version failed for Config Name:{configName} at {DateTime.UtcNow} | _X-Correlation-ID:{CommonHelper.CorrelationID}", ex);
            }
        }

        /// <summary>
        /// This method will perform operations like create README.txt file, update SERIAL.ENC file and Delete PRODUCT.txt file
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="configQueueMessage"></param>
        /// <param name="essFileDownloadPath"></param>
        /// <param name="bessZipFileName"></param>
        /// <returns></returns>
        private async Task PerformAncillaryFilesOperationsAsync(string batchId, ConfigQueueMessage configQueueMessage, string essFileDownloadPath, string bessZipFileName)
        {
            string exchangeSetPath = Path.Combine(homeDirectoryPath, batchId, bessZipFileName);
            string exchangeSetRootPath = Path.Combine(exchangeSetPath, fssApiConfig.Value.EncRoot);
            string readMeFilePath = Path.Combine(exchangeSetRootPath, fssApiConfig.Value.ReadMeFileName);
            await CreateReadMeFileAsync(batchId, configQueueMessage.CorrelationId, configQueueMessage.ReadMeSearchFilter, exchangeSetRootPath, readMeFilePath);

            string exchangeSetInfoPath = Path.Combine(essFileDownloadPath, bessZipFileName, fssApiConfig.Value.Info);
            string serialFilePath = Path.Combine(essFileDownloadPath, bessZipFileName, fssApiConfig.Value.SerialFileName);
            string productFilePath = Path.Combine(essFileDownloadPath, bessZipFileName, fssApiConfig.Value.Info, fssApiConfig.Value.ProductFileName);

            await UpdateSerialFileAsync(serialFilePath, configQueueMessage.Type, configQueueMessage.CorrelationId);

            await DeleteProductTxtAndInfoFolderAsync(productFilePath, exchangeSetInfoPath, configQueueMessage.CorrelationId);
        }

        /// <summary>
        ///     This method will create/download README.txt file based on ReadmeSearchFilter.
        /// </summary>
        /// <param name="batchId"></param>
        /// <param name="correlationId"></param>
        /// <param name="readMeSearchFilter"></param>
        /// <param name="exchangeSetRootPath"></param>
        /// <param name="readMeFilePath"></param>
        /// <returns></returns>
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
                await DownloadReadMeFileAsync(exchangeSetRootPath, correlationId, readMeSearchFilter);
            }
        }

        /// <summary>
        ///     This method will download README.txt file from FSS on ReadmeSearchFilter
        /// </summary>
        /// <param name="exchangeSetRootPath"></param>
        /// <param name="correlationId"></param>
        /// <param name="readMeSearchFilter"></param>
        /// <returns></returns>
        private async Task<bool> DownloadReadMeFileAsync(string exchangeSetRootPath, string correlationId, string readMeSearchFilter)
        {
            bool isDownloadReadMeFileSuccess = false;
            string readMeFilePath = await logger.LogStartEndAndElapsedTimeAsync(EventIds.QueryFileShareServiceReadMeFileRequestStart,
                  EventIds.QueryFileShareServiceReadMeFileRequestCompleted,
                  "File share service search query request for readme.txt file for _X-Correlation-ID:{CorrelationId}",
                  async () => await fssService.SearchReadMeFilePathAsync(correlationId, readMeSearchFilter),
               correlationId);

            if (!string.IsNullOrWhiteSpace(readMeFilePath))
            {
                isDownloadReadMeFileSuccess = await logger.LogStartEndAndElapsedTimeAsync(EventIds.DownloadReadMeFileRequestStart,
                   EventIds.DownloadReadMeFileRequestCompleted,
                   "File share service download request for readme.txt file for _X-Correlation-ID:{CorrelationId}",
                   async () => await fssService.DownloadReadMeFileAsync(readMeFilePath, exchangeSetRootPath, correlationId),
                   correlationId);
            }

            return isDownloadReadMeFileSuccess;
        }

        /// <summary>
        ///     This method will update SERIAL.ENC file from batch.
        /// </summary>
        /// <param name="serialFilePath"></param>
        /// <param name="exchangeSetType"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        /// <exception cref="FulfilmentException"></exception>
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
                logger.LogError(EventIds.BessSerialEncUpdateFailed.ToEventId(), "SERIAL.ENC file update operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.BessSerialEncUpdateFailed.ToEventId());
            }

            await Task.CompletedTask;
        }

        /// <summary>
        ///     This method will delete INFO folder and PRODUCTS.txt file from batch.
        /// </summary>
        /// <param name="productFilePath"></param>
        /// <param name="infoFolderPath"></param>
        /// <param name="correlationId"></param>
        /// <returns></returns>
        /// <exception cref="FulfilmentException"></exception>
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
                logger.LogError(EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId(), "PRODUCT.TXT file and INFO folder delete operation failed at {DateTime} | {ErrorMessage} | _X-Correlation-ID:{CorrelationId}", DateTime.UtcNow, ex.Message, CommonHelper.CorrelationID);
                throw new FulfilmentException(EventIds.BessProductTxtAndInfoFolderDeleteFailed.ToEventId());
            }

            await Task.CompletedTask;
        }

        // This method is for mock only
        [ExcludeFromCodeCoverage]
        private async Task<bool> IsBatchCreatedForMock(ConfigQueueMessage configQueueMessage, string essFileDownloadPath)
        {
            var productVersionEntities = await azureTableStorageHelper.GetLatestBessProductVersionDetailsAsync();

            var productVersions = GetProductVersionsFromEntities(productVersionEntities, configQueueMessage.EncCellNames,
            configQueueMessage.Name, configQueueMessage.ExchangeSetStandard);

            var product = productVersions.Any(x => x.EditionNumber > 0);
            if (product)
            {
                configQueueMessage.Type = "EMPTY";
            }
            bool isBatchCreated = CreateBessBatchAsync(essFileDownloadPath, BESSBATCHFILEEXTENSION, configQueueMessage).Result;

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
                }

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

        private void RenameFile(string downloadPath, List<FssBatchFile> files, string bessZipFileName)
        {
            foreach (var file in files)
            {
                IFileInfo fileInfo = fileSystemHelper.GetFileInfo(Path.Combine(downloadPath, file.FileName));
                if (fileInfo != null)
                {
                    file.FileName = file.FileName.Replace(fssApiConfig.Value.BespokeExchangeSetFileFolder, bessZipFileName);
                }
                fileInfo.MoveTo(Path.Combine(downloadPath, file.FileName));
            }
        }

        [ExcludeFromCodeCoverage]
        private static List<ProductKeyServiceRequest> ProductKeyServiceRequest(ProductVersionsRequest latestProductVersions)
        {
            List<ProductKeyServiceRequest> productKeyServiceRequest = new();

            productKeyServiceRequest.AddRange(latestProductVersions.ProductVersions.Select(
                item => new ProductKeyServiceRequest
                {
                    ProductName = item.ProductName,
                    Edition = item.EditionNumber.ToString()
                }));
            return productKeyServiceRequest;
        }
    }
}
