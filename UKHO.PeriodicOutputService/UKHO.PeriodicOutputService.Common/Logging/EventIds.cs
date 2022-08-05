using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Logging
{
    public enum EventIds
    {
        /// <summary>
        /// 809002 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        UnhandledException = 809002,
        /// <summary>
        /// 809000 - Periodic Output Service webjob request started 
        /// </summary>
        PosFulfilmentJobStarted = 809000,
        /// <summary>
        /// 809001 - Periodic Output Service webjob request completed 
        /// </summary>
        PosFulfilmentJobCompleted = 809001,
        /// <summary>
        /// 809015 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FullAvcsExchangeSetCreationStarted = 809002,
        /// <summary>
        /// 809016 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FullAvcsExchangeSetCreationCompleted = 809002,
        /// <summary>
        /// 809003 - Getting fleet manager access token started
        /// </summary>
        GetFleetMangerAuthTokenStarted = 809003,
        /// <summary>
        /// 809009 - Getting fleet manager access token failed
        /// </summary>
        GetFleetMangerAuthTokenFailed = 809009,
        /// <summary>
        /// 809010 - Getting fleet manager access token completed
        /// </summary>
        GetFleetMangerAuthTokenCompleted = 809010,
        /// <summary>
        /// 809011 - Getting fleet manager catalogue started
        /// </summary>
        GetFleetMangerCatalogueStarted = 809011,
        /// <summary>
        /// 809012 - Getting fleet manager catalogue failed
        /// </summary>
        GetFleetMangerCatalogueFailed = 809012,
        /// <summary>
        /// 809013 - Getting fleet manager catalogue completed
        /// </summary>
        GetFleetMangerCatalogueCompleted = 809013,
        /// <summary>
        /// 809006 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetAccessTokenStarted = 809002,
        /// <summary>
        /// 809007 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        CachedAccessTokenFound = 809002,
        /// <summary>
        /// 809008 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetNewAccessTokenStarted = 809002,
        /// <summary>
        /// 809009 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetNewAccessTokenCompleted = 809002,
        /// <summary>
        /// 809010 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        CachingExternalEndPointTokenStarted = 809002,
        /// <summary>
        /// 809011 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        CachingExternalEndPointTokenCompleted = 809002,
        /// <summary>
        /// 809012 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetAccessTokenCompleted = 809002,
        /// <summary>
        /// 809003 - Creation of full AVCS exchange set started
        /// </summary>
        PostProductIdentifiersToEssStarted = 809003,
        /// <summary>
        /// 809004 - Full AVCS exchange set created successfully
        /// </summary>
        PostProductIdentifiersToEssFailed = 809004,
        /// <summary>
        /// 809005 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        PostProductIdentifiersToEssCompleted = 809002,
        /// <summary>
        /// 809014 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        BatchCreatedInESS = 809002,
        /// <summary>
        /// 809014 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FssBatchDetailUrlNotFound = 809002,
        /// <summary>
        /// 809017 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FssBatchStatusPollingStarted = 809002,
        /// <summary>
        /// 809018 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetBatchStatusRequestStarted = 809002,
        /// <summary>
        /// 809018 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetBatchStatusRequestCompleted = 809002,
        /// <summary>
        /// 809018 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        GetBatchStatusRequestFailed = 809002,
        /// <summary>
        /// 809019 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FssBatchStatusPollingStopped = 809002,
        /// <summary>
        /// 809020 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        FssBatchStatusPollingCompleted = 809002,
        /// <summary>
        /// 809024 - getting batch details started
        /// </summary>
        GetBatchDetailRequestStarted = 809024,
        /// <summary>
        /// 809025 - getting batch details failed
        /// </summary>
        GetBatchDetailRequestFailed = 809025,
        /// <summary>
        /// 809026 - getting batch details completed
        /// </summary>
        GetBatchDetailRequestCompleted = 809026,
        /// <summary>
        /// 809020 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        ErrorFileFoundInBatch = 809002,
        /// <summary>
        /// 809039 - Create batch started
        /// </summary>
        CreateBatchStarted = 809039,
        /// <summary>
        /// 809040 - Create batch failed
        /// </summary>
        CreateBatchFailed = 809040,
        /// <summary>
        /// 809041 - Create batch completed
        /// </summary>
        CreateBatchCompleted = 809041,
        /// <summary>
        /// 809020 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        AddFileToBatchRequestStarted = 809042,
        /// <summary>
        /// 809043 - Add file to batch failed
        /// </summary>
        AddFileToBatchRequestFailed = 809043,
        /// <summary>
        /// 809044 - Add file to batch completed
        /// </summary>
        AddFileToBatchRequestCompleted = 809044,
        /// <summary>
        /// 809033 - write file block started
        /// </summary>
        WriteBlockToFileStarted = 809033,
        /// <summary>
        /// 809034 - write file block failed
        /// </summary>
        WriteBlockToFileFailed = 809034,
        /// <summary>
        /// 809035 - write file block completed
        /// </summary>
        WriteBlockToFileCompleted = 809035,
        /// <summary>
        /// 809030 - upload file block started
        /// </summary>
        UploadFileBlockStarted = 809030,
        /// <summary>
        /// 809031 - upload file block failed
        /// </summary>
        UploadFileBlockFailed = 809031,
        /// <summary>
        /// 809032 - upload file block completed
        /// </summary>
        UploadFileBlockCompleted = 809032,
        /// <summary>
        /// 809036 - commit batch started
        /// </summary>
        CommitBatchStarted = 809036,
        /// <summary>
        /// 809037 - commit batch failed
        /// </summary>
        CommitBatchFailed = 809037,
        /// <summary>
        /// 809038 - commit batch completed
        /// </summary>
        CommitBatchCompleted = 809038,
        /// <summary>
        /// 809038 - commit batch completed
        /// </summary>
        FssPollingCutOffTimeout = 809038,
        /// <summary>
        /// 809037 - commit batch failed
        /// </summary>
        EmptyBatchIdFound = 809037,
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId) => new((int)eventId, eventId.ToString());
    }
}
