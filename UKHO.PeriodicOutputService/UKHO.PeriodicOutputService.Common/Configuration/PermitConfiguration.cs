using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class PermitConfiguration
    {
        public string PermitDecryptionHardwareId { get; set; } = string.Empty;
    }
}
