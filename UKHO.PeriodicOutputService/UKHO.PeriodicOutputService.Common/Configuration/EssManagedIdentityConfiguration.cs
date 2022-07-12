using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    public class EssManagedIdentityConfiguration
    {
        [ExcludeFromCodeCoverage]
        public double DeductTokenExpiryMinutes { get; set; }
    }
}
