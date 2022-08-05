namespace UKHO.PeriodicOutputService.API.FunctionalTests.Models
{
    public class GetBatchResponseModel
    {
        public string BatchId { get; set; }

        public string Status { get; set; }

        public IEnumerable<BatchFile> Files { get; set; }
    }

    public class BatchFile
    {
        public string Filename { get; set; }

        public long FileSize { get; set; }

        public string MimeType { get; set; }

        public string Hash { get; set; }

        public IEnumerable<Attribute> Attributes { get; set; }

        public BatchFileLinks Links { get; set; }
    }
    public class BatchFileLinks
    {
        public Link Get { get; set; }
    }

    public class Link
    {
        public string Href { get; set; }
    }
}
