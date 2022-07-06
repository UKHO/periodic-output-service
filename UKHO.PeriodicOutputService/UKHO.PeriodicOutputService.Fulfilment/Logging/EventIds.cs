using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Fulfilment.Logging
{
    public enum EventIds
    {
        /// <summary>
        /// 809000 - Periodic Output Service webjob request started 
        /// </summary
        POSRequestStarted = 809000,
        /// <summary>
        /// 809001 - Periodic Output Service webjob request completed 
        /// </summary
        POSRequestCompleted = 809001,
        /// <summary>
        /// 809002 - An unhandled Periodic Output Service webjob exception occurred while processing the request.
        /// </summary>
        UnhandledException = 809002,
        /// <summary>
        /// 809003 - Getting access token for ess endpoint started
        /// </summary
        AccessTokenForESSEndpointStarted = 809003,
        /// <summary>
        /// 809004 - Getting access token for ess endpoint completed
        /// </summary
        AccessTokenForESSEndpointCompleted = 809004,
        /// <summary>
        /// 809005 - Request for exchange set details started
        /// </summary
        ExchangeSetRequestStarted = 809005,
        /// <summary>
        /// 809006 - Request for exchange set details completed
        /// </summary
        ExchangeSetRequestCompleted = 809006,
        /// <summary>
        /// 809007 - An exception occured while requesting exchange set
        /// </summary
        ExceptionInExchangeSetRequest = 809007,
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
