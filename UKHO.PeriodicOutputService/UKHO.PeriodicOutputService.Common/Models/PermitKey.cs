using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class PermitKey
    {
        public string ActiveKey { get; set; }
        public string NextKey { get; set; }

    }
}
