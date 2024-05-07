using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Logging
{
    public enum EventIds
    {
        /// <summary>
        /// 809000 - An unhandled exception occurred while processing the request
        /// </summary>
        UnhandledException = 809000,

        /// <summary>
        /// 809001 - Periodic output service webjob started
        /// </summary>
        PosFulfilmentJobStarted = 809001,

        /// <summary>
        /// 809002 - Periodic output service webjob completed
        /// </summary>
        PosFulfilmentJobCompleted = 809002,

        /// <summary>
        /// 809003 - Full AVCS exchangeset creation started
        /// </summary>
        FullAvcsExchangeSetCreationStarted = 809003,

        /// <summary>
        /// 809004 - Full AVCS exchangeset creation completed
        /// </summary>
        FullAvcsExchangeSetCreationCompleted = 809004,

        /// <summary>
        /// 809005 - Getting fleet manager access token started
        /// </summary>
        GetFleetMangerAuthTokenStarted = 809005,

        /// <summary>
        /// 809006 - Getting fleet manager access token failed
        /// </summary>
        GetFleetMangerAuthTokenFailed = 809006,

        /// <summary>
        /// 809007 - Getting fleet manager access token completed
        /// </summary>
        GetFleetMangerAuthTokenCompleted = 809007,

        /// <summary>
        /// 809008 - Getting fleet manager catalogue started
        /// </summary>
        GetFleetMangerCatalogueStarted = 809008,

        /// <summary>
        /// 809009 - Getting fleet manager catalogue failed
        /// </summary>
        GetFleetMangerCatalogueFailed = 809009,

        /// <summary>
        /// 809010 - Getting fleet manager catalogue completed
        /// </summary>
        GetFleetMangerCatalogueCompleted = 809010,

        /// <summary>
        /// 809011 - Get access token to call external api started.
        /// </summary>
        GetAccessTokenStarted = 809011,

        /// <summary>
        /// 809012 - Cached access token to call external api found.
        /// </summary>
        CachedAccessTokenFound = 809012,

        /// <summary>
        /// 809013 - Get new access token to call external api started.
        /// </summary>
        GetNewAccessTokenStarted = 809013,

        /// <summary>
        /// 809014 - Get new access token to call external api completed.
        /// </summary>
        GetNewAccessTokenCompleted = 809014,

        /// <summary>
        /// 809015 - Caching access token to call external api started .
        /// </summary>
        CachingExternalEndPointTokenStarted = 809015,

        /// <summary>
        /// 809016 - Caching access token to call external api completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 809016,

        /// <summary>
        /// 809017 - Get access token to call external api completed.
        /// </summary>
        GetAccessTokenCompleted = 809017,

        /// <summary>
        /// 809018 - Request to post product identifiers to Ess started
        /// </summary>
        PostProductIdentifiersToEssStarted = 809018,

        /// <summary>
        /// 809019 - Request to post product identifiers to Ess failed
        /// </summary>
        PostProductIdentifiersToEssFailed = 809019,

        /// <summary>
        /// 809020 - Request to post product identifiers to Ess completed
        /// </summary>
        PostProductIdentifiersToEssCompleted = 809020,

        /// <summary>
        /// 809021 - ESS Batch created in FSS.
        /// </summary>
        BatchCreatedInESS = 809021,

        /// <summary>
        /// 809022 - ESS validation failed.
        /// </summary>
        EssValidationFailed = 809022,

        /// <summary>
        /// 809023 - Fss batch status polling started.
        /// </summary>
        FssBatchStatusPollingStarted = 809023,

        /// <summary>
        /// 809024 - Get batch status request started.
        /// </summary>
        GetBatchStatusRequestStarted = 809024,

        /// <summary>
        /// 809025 - Get batch status request completed.
        /// </summary>
        GetBatchStatusRequestCompleted = 809025,

        /// <summary>
        /// 809026 - Get batch status request failed.
        /// </summary>
        GetBatchStatusRequestFailed = 809026,

        /// <summary>
        /// 809027 - Fss batch status polling stopped.
        /// </summary>
        FssBatchStatusPollingStopped = 809027,

        /// <summary>
        /// 809028 -  Fss batch status polling completed.
        /// </summary>
        FssBatchStatusPollingCompleted = 809028,

        /// <summary>
        /// 809029 - Get batch details request started
        /// </summary>
        GetBatchDetailRequestStarted = 809029,

        /// <summary>
        /// 809030 -  Get batch details request failed
        /// </summary>
        GetBatchDetailRequestFailed = 809030,

        /// <summary>
        /// 809031 - Get batch details request completed
        /// </summary>
        GetBatchDetailRequestCompleted = 809031,

        /// <summary>
        /// 809032 - Download file started
        /// </summary>
        DownloadFileStarted = 809032,

        /// <summary>
        /// 809033 - Download file failed
        /// </summary>
        DownloadFileFailed = 809033,

        /// <summary>
        /// 809034 - Download file completed
        /// </summary>
        DownloadFileCompleted = 809034,

        /// <summary>
        /// 809035 - Error.txt found in batch.
        /// </summary>
        ErrorFileFoundInBatch = 809035,

        /// <summary>
        /// 809036 - extracting zip file started
        /// </summary>
        ExtractZipFileStarted = 809036,

        /// <summary>
        /// 809037 - extracting zip file failed
        /// </summary>
        ExtractZipFileFailed = 809037,

        /// <summary>
        /// 809038 - extracting zip file completed
        /// </summary>
        ExtractZipFileCompleted = 809038,

        /// <summary>
        /// 809039 - creating iso and sha1 file started
        /// </summary>
        CreateIsoAndSha1Started = 809039,

        /// <summary>
        /// 809040 - creating iso and sha1 file failed
        /// </summary>
        CreateIsoAndSha1Failed = 809040,

        /// <summary>
        /// 809041 - creating iso and sha1 file completed
        /// </summary>
        CreateIsoAndSha1Completed = 809041,

        /// <summary>
        /// 809042 - Fss polling cutoff timeout
        /// </summary>
        FssPollingCutOffTimeout = 809042,

        /// <summary>
        /// 809043 - Empty BatchId found
        /// </summary>
        EmptyBatchIdFound = 809043,

        /// <summary>
        /// 809044 -  Fss batch status polling completed.
        /// </summary>
        FssBatchStatusPollingTimedOut = 809044,

        //////Create Batch events
        /// <summary>
        /// 809045 - Create batch started
        /// </summary>
        CreateBatchStarted = 809045,

        /// <summary>
        /// 809046 - Create batch failed
        /// </summary>
        CreateBatchFailed = 809046,

        /// <summary>
        /// 809047 - Create batch completed
        /// </summary>
        CreateBatchCompleted = 809047,

        /// <summary>
        /// 809048 - Add file to batch request started
        /// </summary>
        AddFileToBatchRequestStarted = 809048,

        /// <summary>
        /// 809049 - Add file to batch request failed
        /// </summary>
        AddFileToBatchRequestFailed = 809049,

        /// <summary>
        /// 809050 - Add file to batch request completed
        /// </summary>
        AddFileToBatchRequestCompleted = 809050,

        /// <summary>
        /// 809051 - Write file block started
        /// </summary>
        WriteBlockToFileStarted = 809051,

        /// <summary>
        /// 809052 - Write file block failed
        /// </summary>
        WriteBlockToFileFailed = 809052,

        /// <summary>
        /// 809053 - Write file block completed
        /// </summary>
        WriteBlockToFileCompleted = 809053,

        /// <summary>
        /// 809054 - Upload file block started
        /// </summary>
        UploadFileBlockStarted = 809054,

        /// <summary>
        /// 809055 - Upload file block failed
        /// </summary>
        UploadFileBlockFailed = 809055,

        /// <summary>
        /// 809056 - Upload file block completed
        /// </summary>
        UploadFileBlockCompleted = 809056,

        /// <summary>
        /// 809057 - Commit batch started
        /// </summary>
        CommitBatchStarted = 809057,

        /// <summary>
        /// 809058 - Commit batch failed
        /// </summary>
        CommitBatchFailed = 809058,

        /// <summary>
        /// 809059 - Commit batch completed
        /// </summary>
        CommitBatchCompleted = 809059,

        /// <summary>
        /// 809060 - Update exchangeset creation started
        /// </summary>
        UpdateExchangeSetCreationStarted = 809060,

        /// <summary>
        /// 809061 - Update exchangeset creation completed
        /// </summary>
        UpdateExchangeSetCreationCompleted = 809061,

        /// <summary>
        /// 809062 - Get product data since given datetime started
        /// </summary>
        GetProductDataSinceDateTimeStarted = 809062,

        /// <summary>
        /// 809063 -  Get product data since given datetime completed
        /// </summary>
        GetProductDataSinceDateTimeCompleted = 809063,

        /// <summary>
        /// 809064 -  Get product data since given datetime failed
        /// </summary>
        GetProductDataSinceDateTimeFailed = 809064,

        /// <summary>
        /// 809065 -  Batch creation for catalogue completed
        /// </summary>
        BatchCreationForCatalogueCompleted = 809065,

        /// <summary>
        /// 809066 -  Batch creation for ENC update completed
        /// </summary>
        BatchCreationForENCUpdateCompleted = 809066,

        /// <summary>
        /// 809067 -  Get latest since datetime started.
        /// </summary>
        GetLatestSinceDateTimeStarted = 809067,

        /// <summary>
        /// 809068 -  Get latest since datetime started.
        /// </summary>
        GetLatestSinceDateTimeCompleted = 809068,

        /// <summary>
        /// 809069 -  Logging History started.
        /// </summary>
        LoggingHistoryStarted = 809069,

        /// <summary>
        /// 809070 -  Logging History completed.
        /// </summary>
        LoggingHistoryCompleted = 809070,

        /// <summary>
        /// 809071 -  Logging History failed.
        /// </summary>
        LoggingHistoryFailed = 809071,

        /// <summary>
        /// 809072 - Exchange set not modified
        /// </summary>
        ExchangeSetNotModified = 809072,

        /// <summary>
        /// 809073 - Cancelled products found in ESS repsonse
        /// </summary>
        CancelledProductsFound = 809073,

        /// <summary>
        /// 809074 - AIO fulfilment job started
        /// </summary>
        AIOFulfilmentJobStarted = 809074,

        /// <summary>
        /// 809075 - AIO fulfilment job completed
        /// </summary>
        AIOFulfilmentJobCompleted = 809075,

        ///<summary>
        ///809076 - AIO Base Exchange Set creation started
        ///</summary>
        AioBaseExchangeSetCreationStarted = 809076,

        ///<summary>
        ///809077 - AIO Base Exchange Set creation Completed
        ///</summary>
        AioBaseExchangeSetCreationCompleted = 809077,

        /// <summary>
        /// 809078 - AIO Get info folder files not found
        /// </summary>
        GetAioInfoFolderFilesNotFound = 809078,

        /// <summary>
        /// 809079 - AIO Get info folder files found
        /// </summary>
        GetAioInfoFolderFilesOkResponse = 809079,

        /// <summary>
        /// 809080 - AIO Get info folder files status not ok
        /// </summary>
        GetAioInfoFolderFilesNonOkResponse = 809080,

        /// <summary>
        ///  809081 - AIO Base Exchange Set Ancillary Files download started
        /// </summary>
        AioAncillaryFilesDownloadStarted = 809081,

        /// <summary>
        ///  809082 - AIO Base Exchange Set Ancillary Files download completed
        /// </summary>
        AioAncillaryFilesDownloadCompleted = 809082,

        /// <summary>
        ///  809081 - AIO Base Exchange Set Ancillary Files not found
        /// </summary>
        AioAncillaryFilesNotFound = 809083,

        ///<summary>
        ///809084 - AIO Cells Configuration Missing
        ///</summary>
        AioCellsConfigurationMissing = 809084,

        /// <summary>
        /// 809085 - creating zip file started
        /// </summary>
        ZipFileCreationStarted = 809085,

        /// <summary>
        /// 809086 - creating zip file failed
        /// </summary>
        ZipFileCreationFailed = 809086,

        /// <summary>
        /// 809087 - creating zip file completed
        /// </summary>
        ZipFileCreationCompleted = 809087,

        ///<summary>
        /// 809088 - AIO Update Exchange Set Creation Started
        ///</summary>
        AioUpdateExchangeSetCreationStarted = 809088,

        ///<summary>
        /// 809089 - AIO Update Exchange Set Creation Completed
        ///</summary>
        AioUpdateExchangeSetCreationCompleted = 809089,

        /// <summary>
        /// 809090 - Get product data for given product version started
        /// </summary>
        GetProductDataProductVersionStarted = 809090,

        /// <summary>
        /// 809091 -  Get product data for given product version completed
        /// </summary>
        GetProductDataProductVersionCompleted = 809091,

        /// <summary>
        /// 809092 -  Get product data for given product version failed
        /// </summary>
        GetProductDataProductVersionFailed = 809092,

        /// <summary>
        /// 809093 -  Logging Product Versions started
        /// </summary>
        LoggingProductVersionsStarted = 809093,

        /// <summary>
        /// 809094 -  Logging Product Versions Completed
        /// </summary>
        LoggingProductVersionsCompleted = 809094,

        /// <summary>
        /// 809095 -  Logging Product Versions failed
        /// </summary>
        LoggingProductVersionsFailed = 809095,

        /// <summary>
        /// 809096 -  Get latest product version details started.
        /// </summary>
        GetLatestProductVersionDetailsStarted = 809096,

        /// <summary>
        /// 809097 -  Get latest product version details completed.
        /// </summary>
        GetLatestProductVersionDetailsCompleted = 809097,

        /// <summary>
        /// 809098 -  It reflects the configuration of the AIO cell is not synchronized with the ESS. V01X01 file found in AIO batch
        /// </summary>
        V01X01FileFoundInAIOBatch = 809098,

        /// <summary>
        /// 809099 - Bess Configuration Service Started
        /// </summary>
        BessConfigurationServiceStarted = 809099,

        /// <summary>
        /// 809100 - Bess Configuration Service Completed
        /// </summary>
        BessConfigurationServiceCompleted = 809100,

        /// <summary>
        /// 809101 - Bess configs Processing Started
        /// </summary>
        BessConfigsProcessingStarted = 809101,

        /// <summary>
        /// 809102 - Bess configs Processing Started
        /// </summary>
        BessConfigsProcessingCompleted = 809102,

        /// <summary>
        /// 809103 - Bess configs Processing Failed
        /// </summary>
        BessConfigsProcessingFailed = 809103,

        /// <summary>
        /// 809104 - Bess config value is not defined
        /// </summary>
        BessConfigValueNotDefined = 809104,

        /// <summary>
        /// 809105 - Bess config parsing error
        /// </summary>
        BessConfigParsingError = 809105,

        /// <summary>
        /// 809106 - Bess configs not found
        /// </summary>
        BessConfigsNotFound = 809106,

        /// <summary>
        /// 809107 - Bess error occurred while downloading configs of json file from azure storage container
        /// </summary>
        BessErrorOccurredWhileDownloadingConfigFromAzureStorage = 809107,

        /// <summary>
        /// 809108 - Bess config invalid attributes
        /// </summary>
        BessConfigInvalidAttributes = 809108,

        /// <summary>
        /// 809109 - Bess config duplicate Name attribute
        /// </summary>
        BessConfigDuplicateFileCount = 809109,

        /// <summary>
        /// 809110 - Bess config validation summary
        /// </summary>
        BessConfigValidationSummary = 809110,

        /// <summary>
        /// 809111 - Bess config duplicate records found
        /// </summary>
        BessConfigsDuplicateRecordsFound = 809111,

        /// <summary>
        /// 809112 - Bess config frequency due or lapsed
        /// </summary>
        BessConfigFrequencyElapsed = 809112,

        /// <summary>
        /// 809113 - Exception occurred while processing bess config frequency
        /// </summary>
        BessConfigFrequencyProcessingException = 809113,

        /// <summary>
        /// 809114 - Request to Sales Catalogue Service's catalogue essData endpoint is started.
        /// </summary>
        ScsGetSalesCatalogueDataRequestStarted = 809114,

        /// <summary>
        /// 809115 - Request to Sales Catalogue Service's catalogue essData endpoint is completed.
        /// </summary>
        ScsGetSalesCatalogueDataRequestCompleted = 809115,

        /// <summary>
        /// 809116 - Request to Sales Catalogue Service's catalogue essData endpoint failed due to Non-Ok response.
        /// </summary>
        ScsGetSalesCatalogueDataNonOkResponse = 809116,

        /// <summary>
        /// 809117 - Request for retrying sales catalogue service endpoint.
        /// </summary>
        RetryHttpClientScsRequest = 809117,

        /// <summary>
        /// 809118 - Bess Builder Service Started
        /// </summary>
        BessBuilderServiceStarted = 809118,

        /// <summary>
        /// 809119 - Bess Builder Service Completed
        /// </summary>
        BessBuilderServiceCompleted = 809119,

        /// <summary>
        /// 809120 - Listed cells or pattern not found in catalogue
        /// </summary>
        BessInvalidEncCellNamesOrPatternNotFoundInSalesCatalogue = 809120,

        /// <summary>
        /// 809121 - All listed cells are not found and neither cell is matching with the pattern
        /// </summary>
        BessEncCellNamesAndPatternNotFoundInSalesCatalogue = 809121,

        /// <summary>
        /// 809122 - Config message added in the queue
        /// </summary>
        BessConfigPropertiesAddedInQueue = 809122,

        /// <summary>
        /// 809123 - Bess Config Validation Error
        /// </summary>
        BessConfigValidationError = 809123,

        /// <summary>
        /// 809124 - Bespoke Exchange Set size exceeds threshold 700MB
        /// </summary>
        BessSizeExceedsThreshold = 809124,

        /// <summary>
        /// 809125 - Message added in the queue
        /// </summary>
        BessQueueMessageSuccessful = 809125,

        /// <summary>
        /// 809126 - Message not added in the queue
        /// </summary>
        BessQueueMessageFailed = 809126,

        /// <summary>
        /// 809127 - Creation of bespoke exchange set is started.
        /// </summary>
        CreateBespokeExchangeSetRequestStart = 809127,

        /// <summary>
        /// 809128 - Creation of bespoke exchange set is completed.
        /// </summary>
        CreateBespokeExchangeSetRequestCompleted = 809128,

        /// <summary>
        /// 809129 - Products Fetched from ESS.
        /// </summary>
        ProductsFetchedFromESS = 809129,

        /// <summary>
        /// 809130 - Empty batch response from ESS.
        /// </summary>
        EmptyBatchResponse = 809130,

        /// <summary>
        /// 809131 - Macro is invalid or unavailable.
        /// </summary>
        MacroInvalidOrUnavailable = 809131,

        /// <summary>
        /// 809132 - Exception occurred while transforming macros.
        /// </summary>
        MacroTransformationFailed = 809132,

        /// <summary>
        /// 809133 - Request for searching readme.txt file from file share service is started.
        /// </summary>
        QueryFileShareServiceReadMeFileRequestStart = 809133,

        /// <summary>
        /// 809134 - Request for searching readme.txt from file share service is completed.
        /// </summary>
        QueryFileShareServiceReadMeFileRequestCompleted = 809134,

        /// <summary>
        /// 809135 - Request for searching readme.txt file from file share service is failed due to non ok response.
        /// </summary>
        QueryFileShareServiceReadMeFileNonOkResponse = 809135,

        /// <summary>
        /// 809136 - Readme.txt file is not found while searching in file share service.
        /// </summary>
        ReadMeTextFileNotFound = 809136,

        /// <summary>
        /// 809137 - Request for downloading readme.txt file from file share service is started.
        /// </summary>
        DownloadReadMeFileRequestStart = 809137,

        /// <summary>
        /// 809138 - Request for downloading readme.txt file from file share service is completed.
        /// </summary>
        DownloadReadMeFileRequestCompleted = 809138,

        /// <summary>
        /// 809139 - Request for downloading readme.txt file from file share service is failed due to non ok response.
        /// </summary>
        DownloadReadMeFileNonOkResponse = 809139,

        /// <summary>
        /// 809140 - Completed download of 307 response readme.txt file from the file share service.
        /// </summary>
        DownloadReadmeFile307RedirectResponse = 809140,

        /// <summary>
        /// 809141 - Multiple files found while searching readme.txt file from the file share service.
        /// </summary>
        QueryFileShareServiceMultipleFilesFound = 809141,

        /// <summary>
        /// 809142 - Serial.ENC file updated with Type from configuration
        /// </summary>
        BessSerialEncUpdated = 809142,

        /// <summary>
        /// 809143 - PRODUCT.TXT file and INFO folder deleted
        /// </summary>
        BessProductTxtAndInfoFolderDeleted = 809143,

        /// <summary>
        /// 809144 - SERIAL.ENC file update operation failed
        /// </summary>
        BessSerialEncUpdateFailed = 809144,

        /// <summary>
        /// 809145 - PRODUCT.TXT file and INFO folder delete operation failed
        /// </summary>
        BessProductTxtAndInfoFolderDeleteFailed = 809145,

        /// <summary>
        /// 809146 - Post product data to PKS started.
        /// </summary>
        PostProductKeyDataToPksStarted = 809146,

        /// <summary>
        /// 809147 - Post product data to PKS completed.
        /// </summary>
        PostProductKeyDataToPksCompleted = 809147,

        /// <summary>
        /// 809148 - Post product data to PKS failed.
        /// </summary>
        PostProductKeyDataToPksFailed = 809148
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId) => new((int)eventId, eventId.ToString());
    }
}
