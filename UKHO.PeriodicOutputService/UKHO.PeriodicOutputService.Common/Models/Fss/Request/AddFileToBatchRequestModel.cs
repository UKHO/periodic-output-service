
namespace UKHO.PeriodicOutputService.Common.Models.Request
{
    public class AddFileToBatchRequestModel
    {
        public IEnumerable<KeyValuePair<String, string>> Attributes { get; set; }
    }
}
