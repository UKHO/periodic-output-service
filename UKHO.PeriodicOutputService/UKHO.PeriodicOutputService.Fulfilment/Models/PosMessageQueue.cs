namespace UKHO.PeriodicOutputService.Fulfilment.Models
{
    public class PosMessageQueue
    {
        public string? BatchId { get; set; }
        public long FileSize { get; set; }
        public string? CorrelationId { get; set; }
    }
}
