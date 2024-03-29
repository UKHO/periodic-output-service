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
        public string BatchStatusPollingCutoffTimeForBES { get; set; }
        public string BatchStatusPollingDelayTimeForBES { get; set; }
        public int BlockSizeInMultipleOfKBs { get; set; }
        public int ParallelUploadThreadCount { get; set; }
        public string BusinessUnit { get; set; }
        public string PosReadUsers { get; set; }
        public string PosReadGroups { get; set; }
        public string AioBusinessUnit { get; set; }
        public string AioReadUsers { get; set; }
        public string AioReadGroups { get; set; }
        public string Content { get; set; }
        public string ProductType { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
