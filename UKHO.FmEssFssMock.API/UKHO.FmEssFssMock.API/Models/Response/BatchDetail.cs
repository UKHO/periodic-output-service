namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class BatchDetail
    {
        public string BatchId { get; set; }
        public string Status { get; set; }
        public IEnumerable<Attribute> Attributes { get; set; }
        public string BusinessUnit { get; set; }
        public DateTime? BatchPublishedDate { get; set; }
        public string ExpiryDate { get; set; }
        public IEnumerable<BatchFile> Files { get; set; }
    }
}
