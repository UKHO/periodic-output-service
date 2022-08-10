namespace UKHO.FmEssFssMock.API.Models.Request
{
    public class BatchRequest
    {
        public string BusinessUnit { get; set; }

        public Acl Acl { get; set; }

        public IEnumerable<KeyValuePair<String, string>> Attributes { get; set; }

        public string ExpiryDate { get; set; }
    }

    public class Acl
    {
        public IEnumerable<string> ReadUsers { get; set; }

    }
}
