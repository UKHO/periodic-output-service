using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PksApiConfiguration : IPksApiConfiguration
    {
        public string BaseUrl { get; set; }
        public string ClientId { get; set; }
        public string PermitDecryptionHardwareId { get; set; } = string.Empty;
    }
}
