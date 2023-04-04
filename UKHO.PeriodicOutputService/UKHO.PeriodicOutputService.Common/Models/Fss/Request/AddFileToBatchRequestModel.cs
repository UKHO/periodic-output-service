
namespace UKHO.PeriodicOutputService.Common.Models.Fss.Request
{
    public class AddFileToBatchRequestModel
    {
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
    }
}
