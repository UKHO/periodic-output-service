
namespace UKHO.PeriodicOutputService.Common.Models.Request
{
    public class AddFileToBatchRequestModel
    {
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
    }
}
