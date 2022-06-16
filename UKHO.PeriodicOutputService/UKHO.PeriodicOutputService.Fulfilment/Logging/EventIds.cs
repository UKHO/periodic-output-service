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
    }

    public static class EventIdExtensions
    {
        public static EventId ToEventId(this EventIds eventId)
        {
            return new EventId((int)eventId, eventId.ToString());
        }
    }
}
