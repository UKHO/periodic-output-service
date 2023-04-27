using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class FssApiConfiguration : IFssApiConfiguration
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
        public string FssClientId { get; set; }
        public string BaseUrl { get; set; }
        public string BatchStatusPollingCutoffTime { get; set; }
        public string BatchStatusPollingDelayTime { get; set; }
        public string BatchStatusPollingCutoffTimeForAIO { get; set; }
        public string BatchStatusPollingDelayTimeForAIO { get; set; }
        public int BlockSizeInMultipleOfKBs { get; set; }
        public int ParallelUploadThreadCount { get; set; }
        public string BusinessUnit { get; set; }
        public string PosReadUsers { get; set; }
        public string PosReadGroups { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
