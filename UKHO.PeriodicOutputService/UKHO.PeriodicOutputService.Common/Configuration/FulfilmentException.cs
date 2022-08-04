using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [Serializable]
    public class FulfilmentException : Exception
    {
        public EventId EventId { get; set; }

        public FulfilmentException(EventId eventId)
        {
            EventId = eventId;
        }

        protected FulfilmentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
