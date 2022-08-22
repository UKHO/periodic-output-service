namespace UKHO.FmEssFssMock.API.Models.Request
{
    public class CreateBatchRequest
    {
        public string BusinessUnit { get; set; }
        public Acl Acl { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Attributes { get; set; }
        public string ExpiryDate { get; set; }
    }

    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }
    }
}
