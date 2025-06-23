
namespace UKHO.PeriodicOutputService.Common.Models.Request
{
    public class AddFileRequest
    {
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
    }
}
