using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Logging
{
    public enum EventIds
    {
        /// <summary>
        /// 809000 - Periodic Output Service webjob request started 
        /// </summary
        PosFulfilmentJobStarted = 809000,
        /// <summary>
        /// 809001 - Periodic Output Service webjob request completed 
        /// </summary
        PosFulfilmentJobCompleted = 809001,
        /// <summary>
        /// 809002 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        UnhandledException = 809002,
        /// <summary>
        /// 809003 - Getting access token for ess endpoint started
        /// </summary
        GetAccessTokenForESSEndPointStarted = 809003,
        /// <summary>
        /// 809004 - Getting access token for ess endpoint completed
        /// </summary
        GetAccessTokenForESSEndPointCompleted = 809004,
        /// <summary>
        /// 809005 - Request for exchange set details started
        /// </summary
        ExchangeSetPostProductIdentifiersRequestStarted = 809005,
        /// <summary>
        /// 809006 - Request for exchange set details completed
        /// </summary
        ExchangeSetPostProductIdentifiersRequestCompleted = 809006,
        /// <summary>
        /// 809007 - An exception occured while requesting exchange set
        /// </summary
        ExchangeSetPostProductIdentifiersFailed = 809007,
        /// <summary>
        /// 809008 - Getting fleet manager access token started
        /// </summary
        FleetMangerGetAuthTokenStarted = 809008,
        /// <summary>
        /// 809009 - Getting fleet manager access token failed
        /// </summary
        FleetMangerGetAuthTokenFailed = 809009,
        /// <summary>
        /// 809010 - Getting fleet manager access token completed
        /// </summary
        FleetMangerGetAuthTokenCompleted = 809010,
        /// <summary>
        /// 809011 - Getting fleet manager catalogue started
        /// </summary
        FleetMangerGetCatalogueStarted = 809011,
        /// <summary>
        /// 809012 - Getting fleet manager catalogue failed
        /// </summary
        FleetMangerGetCatalogueFailed = 809012,
        /// <summary>
        /// 809013 - Getting fleet manager catalogue completed
        /// </summary
        FleetMangerGetCatalogueCompleted = 809013,
        /// <summary>
        /// 809014 - Posting productidentifiers to ESS started
        /// </summary
        PostProductIdentifiersStarted = 809014,
        /// <summary>
        /// 809015 - Posting productidentifiers to ESS failed
        /// </summary
        PostProductIdentifiersFailed = 809015,
        /// <summary>
        /// 809016 - Posting productidentifiers to ESS completed
        /// </summary
        PostProductIdentifiersCompleted = 809016,
        /// <summary>
        /// 809017 - Request to get batch status started
        /// </summary
        BatchStatusRequestStarted = 809017,
        /// <summary>
        /// 809018 - Request to get batch status completed
        /// </summary
        BatchStatusRequestCompleted = 809018,
        /// <summary>
        /// 809019 - Request to get batch status failed
        /// </summary
        BatchStatusRequestFailed = 809019,
        /// <summary>
        /// 809020 - Request to get fss auth token started
        /// </summary
        GetFssAuthTokenRequestStarted = 809020,
        /// <summary>
        /// 809021 - Request to get fss auth token completed
        /// </summary
        GetFssAuthTokenRequestCompleted = 809021,
        /// <summary>
        /// 809022 - Request to get fss auth token failed
        /// </summary
        GetFssAuthTokenRequestFailed = 809022,
        /// <summary>
        /// 809023 - caching auth token
        /// </summary
        CachingExternalEndPointToken = 809023
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
