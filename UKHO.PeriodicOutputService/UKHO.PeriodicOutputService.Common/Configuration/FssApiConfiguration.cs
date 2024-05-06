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
        public string BatchStatusPollingCutoffTimeForBESS { get; set; }
        public string BatchStatusPollingDelayTimeForBESS { get; set; }
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
        public string BESSBusinessUnit { get; set; }
        public string BessReadUsers { get; set; }
        public string BessReadGroups { get; set; }
        public string BespokeExchangeSetFileFolder { get; set; }
        public string EncRoot { get; set; }
        public string ReadMeFileName { get; set; }
        public string SerialFileName { get; set; }
        public string ProductFileName { get; set; }
        public string Info { get; set; }
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
