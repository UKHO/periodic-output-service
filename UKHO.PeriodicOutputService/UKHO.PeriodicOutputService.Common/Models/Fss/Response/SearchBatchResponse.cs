namespace UKHO.PeriodicOutputService.Common.Models.Fss.Response
{
    public class SearchBatchResponse
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public List<GetBatchResponseModel> Entries { get; set; }
        public PagingLinks Links { get; set; }
    }

    public class PagingLinks
    {
        public Link Self { get; set; }
        public Link First { get; set; }
        public Link Previous { get; set; }
        public Link Next { get; set; }
        public Link Last { get; set; }
    }
}
