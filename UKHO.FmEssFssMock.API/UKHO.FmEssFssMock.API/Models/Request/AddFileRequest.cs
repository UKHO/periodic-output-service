
namespace UKHO.PeriodicOutputService.Common.Models.Request
{
    public class AddFileRequest
    {
        public IEnumerable<KeyValuePair<String, string>> Attributes { get; set; }
    }
}
