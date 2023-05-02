namespace UKHO.PeriodicOutputService.Common.Models.Fss.Response
{
    public class SearchBatchResponse
    {
        public int Count { get; set; }
        public int Total { get; set; }
        public List<GetBatchResponseModel> Entries { get; set; }
        public PagingLinks Links { get; set; }
    }
}
