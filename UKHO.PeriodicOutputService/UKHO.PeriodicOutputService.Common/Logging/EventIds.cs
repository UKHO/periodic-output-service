using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Logging
{
    public enum EventIds
    {
        /// <summary>
        /// 809000 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        UnhandledException = 809000,
        /// <summary>
        /// 809001 - Periodic Output Service webjob request started 
        /// </summary>
        PosFulfilmentJobStarted = 809001,
        /// <summary>
        /// 809002 - Periodic Output Service webjob request completed 
        /// </summary>
        PosFulfilmentJobCompleted = 809002,
        /// <summary>
        /// 809003 - full avcs exchangeset creation started.
        /// </summary>
        FullAvcsExchangeSetCreationStarted = 809003,
        /// <summary>
        /// 809004 - full avcs exchangeset creation completed.
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
        /// 809011 - Get access token started.
        /// </summary>
        GetAccessTokenStarted = 809011,
        /// <summary>
        /// 809012 - Cached access toeken found.
        /// </summary>
        CachedAccessTokenFound = 809012,
        /// <summary>
        /// 809013 - Get new access token started.
        /// </summary>
        GetNewAccessTokenStarted = 809013,
        /// <summary>
        /// 809014 - Get new access token completed.
        /// </summary>
        GetNewAccessTokenCompleted = 809014,
        /// <summary>
        /// 809015 - Cashing external endpoint token started .
        /// </summary>
        CachingExternalEndPointTokenStarted = 809015,
        /// <summary>
        /// 809016 - Caching external endpoint token completed.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 809016,
        /// <summary>
        /// 809017 - Get access token completed.
        /// </summary>
        GetAccessTokenCompleted = 809017,
        /// <summary>
        /// 809018 - Post product identifiers to Ess started
        /// </summary>
        PostProductIdentifiersToEssStarted = 809018,
        /// <summary>
        /// 809019 - Post product identifiers to Ess failed
        /// </summary>
        PostProductIdentifiersToEssFailed = 809019,
        /// <summary>
        /// 809020 - Post product identifiers to Ess completed
        /// </summary>
        PostProductIdentifiersToEssCompleted = 809020,
        /// <summary>
        /// 809021 - Batch created in ESS.
        /// </summary>
        BatchCreatedInESS = 809021,
        /// <summary>
        /// 809022 - Fss batch details url not found.
        /// </summary>
        FssBatchDetailUrlNotFound = 809022,
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
        /// 809029 - getting batch details started
        /// </summary>
        GetBatchDetailRequestStarted = 809029,
        /// <summary>
        /// 809030 - getting batch details request failed
        /// </summary>
        GetBatchDetailRequestFailed = 809030,
        /// <summary>
        /// 809031 - getting batch details request completed
        /// </summary>
        GetBatchDetailRequestCompleted = 809031,
        /// <summary>
        /// 809032 - downloading file started
        /// </summary>
        DownloadFileStarted = 809032,
        /// <summary>
        /// 809033 - downloading file failed
        /// </summary>
        DownloadFileFailed = 809033,
        /// <summary>
        /// 809034 - downloading file completed
        /// </summary>
        DownloadFileCompleted = 809034,
        /// <summary>
        /// 809035 - Error found in get batch file.
        /// </summary>
        ErrorFileFoundInBatch = 809035,
        /// <summary>
        /// 809036 - Create batch started
        /// </summary>
        CreateBatchStarted = 809036,
        /// <summary>
        /// 809037 - Create batch failed
        /// </summary>
        CreateBatchFailed = 809037,
        /// <summary>
        /// 809038 - Create batch completed
        /// </summary>
        CreateBatchCompleted = 809038,
        /// <summary>
        /// 809039 - Add file to batch request started.
        /// </summary>
        AddFileToBatchRequestStarted = 809039,
        /// <summary>
        /// 809040 - Add file to batch request failed
        /// </summary>
        AddFileToBatchRequestFailed = 809040,
        /// <summary>
        /// 809041 - Add file to batch request completed
        /// </summary>
        AddFileToBatchRequestCompleted = 809041,
        /// <summary>
        /// 809042 - write file block started
        /// </summary>
        WriteBlockToFileStarted = 809042,
        /// <summary>
        /// 809043 - write file block failed
        /// </summary>
        WriteBlockToFileFailed = 809043,
        /// <summary>
        /// 809044 - write file block completed
        /// </summary>
        WriteBlockToFileCompleted = 809044,
        /// <summary>
        /// 809045 - upload file block started
        /// </summary>
        UploadFileBlockStarted = 809045,
        /// <summary>
        /// 809046 - upload file block failed
        /// </summary>
        UploadFileBlockFailed = 809046,
        /// <summary>
        /// 809047 - upload file block completed
        /// </summary>
        UploadFileBlockCompleted = 809047,
        /// <summary>
        /// 809048 - commit batch started
        /// </summary>
        CommitBatchStarted = 809048,
        /// <summary>
        /// 809049 - commit batch failed
        /// </summary>
        CommitBatchFailed = 809049,
        /// <summary>
        /// 809050 - commit batch completed
        /// </summary>
        CommitBatchCompleted = 809050,
        /// <summary>
        /// 809051 - Fss polling cutoff timeout
        /// </summary>
        FssPollingCutOffTimeout = 809051,
        /// <summary>
        /// 809052 - Empty BatchId found
        /// </summary>
        EmptyBatchIdFound = 809052,
        /// <summary>
        /// 809053 -  Fss batch status polling completed.
        /// </summary>
        FssBatchStatusPollingTimedOut = 809053,
        /// <summary>
        /// 809054 - extracting zip file started
        /// </summary>
        ExtractZipFileStarted = 809054,
        /// <summary>
        /// 809055 - extracting zip file failed
        /// </summary>
        ExtractZipFileFailed = 809055,
        /// <summary>
        /// 809056 - extracting zip file completed
        /// </summary>
        ExtractZipFileCompleted = 809056,
        /// <summary>
        /// 809057 - creating iso and sha1 file started
        /// </summary>
        CreateIsoAndSha1Started = 809057,
        /// <summary>
        /// 809058 - creating iso and sha1 file failed
        /// </summary>
        CreateIsoAndSha1Failed = 809058,
        /// <summary>
        /// 809059 - creating iso and sha1 file completed
        /// </summary>
        CreateIsoAndSha1Completed = 809059,
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId) => new((int)eventId, eventId.ToString());
    }
}
