
namespace UKHO.PeriodicOutputService.Fulfilment.Services
{
    public interface IFssBatchService
    {
        public Task<string> CheckIfBatchCommitted(string url);
    }
}
