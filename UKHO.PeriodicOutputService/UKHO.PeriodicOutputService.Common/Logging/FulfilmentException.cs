using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;
using Microsoft.Extensions.Logging;

namespace UKHO.PeriodicOutputService.Common.Logging
{

    [ExcludeFromCodeCoverage]
    [Serializable]
    public class FulfilmentException(EventId eventId) : Exception()
    {
        public EventId EventId { get; set; } = eventId;
    }
}
