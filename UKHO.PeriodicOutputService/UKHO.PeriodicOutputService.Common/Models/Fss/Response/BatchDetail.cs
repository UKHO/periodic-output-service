using System.Text.Json.Serialization;

namespace UKHO.PeriodicOutputService.Common.Models.Fss.Response
{
    public class BatchDetail
    {
        public string BatchId { get; set; }

        public string Status { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public string BusinessUnit { get; set; }

        public DateTime? BatchPublishedDate { get; set; }

        public DateTime? ExpiryDate { get; set; }

        public IEnumerable<BatchFile> Files { get; set; }

        [JsonIgnore]
        public bool IgnoreCache { get; set; }
    }

    public class BatchFile
    {
        public string Filename { get; set; }

        public long FileSize { get; set; }

        public string MimeType { get; set; }

        public string Hash { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public Links Links { get; set; }
    }

    public class Attribute
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public class Links
    {
        public Link Get { get; set; }
    }

    public class Link
    {
        public string Href { get; set; }
    }
}
