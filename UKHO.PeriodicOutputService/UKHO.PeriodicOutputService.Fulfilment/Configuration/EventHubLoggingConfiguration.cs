using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Fulfilment.Configuration
{
    [ExcludeFromCodeCoverage]
    public class EventHubLoggingConfiguration
    {
        public string MinimumLoggingLevel { get; set; } = "Default Key";
        public string UkhoMinimumLoggingLevel { get; set; } = "Default Key";
        public string Environment { get; set; } = "Default Key";
        public string EntityPath { get; set; } = "Default Key";
        public string System { get; set; } = "Default Key";
        public string Service { get; set; } = "Default Key";
        public string NodeName { get; set; } = "Default Key";
        public string ConnectionString { get; set; } = "Default Key";
    }
}
