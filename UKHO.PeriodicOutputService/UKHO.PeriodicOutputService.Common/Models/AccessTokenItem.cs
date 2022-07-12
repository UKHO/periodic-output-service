using System.Diagnostics.CodeAnalysis;

namespace UKHO.PeriodicOutputService.Common.Models
{
    [ExcludeFromCodeCoverage]
    public class AccessTokenItem
    {
        public string AccessToken { get; set; } = string.Empty;
        public DateTime ExpiresIn { get; set; }
    }
}
