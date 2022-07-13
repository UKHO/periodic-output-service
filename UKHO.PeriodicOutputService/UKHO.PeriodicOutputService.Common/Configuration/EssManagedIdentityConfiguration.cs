using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class EssManagedIdentityConfiguration
    {
        public double DeductTokenExpiryMinutes { get; set; }
    }
}
