
namespace UKHO.PeriodicOutputService.Common.Models.Fss.Request
{
    public class CreateBatchRequestModel
    {
        public string BusinessUnit { get; set; }

        public Acl Acl { get; set; }

        public IList<KeyValuePair<string, string>> Attributes { get; set; }

        public string ExpiryDate { get; set; }
    }
    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }
        public IEnumerable<string> ReadGroups { get; set; }

    }
}
