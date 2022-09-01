
namespace UKHO.PeriodicOutputService.Common.Models.Request
{
    public class CreateBatchRequestModel
    {
        public string BusinessUnit { get; set; }

        public Acl Acl { get; set; }

        public IEnumerable<KeyValuePair<String, string>> Attributes { get; set; }

        public string ExpiryDate { get; set; }
    }
    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }
        public IEnumerable<string> ReadGroups { get; set; }

    }
}
