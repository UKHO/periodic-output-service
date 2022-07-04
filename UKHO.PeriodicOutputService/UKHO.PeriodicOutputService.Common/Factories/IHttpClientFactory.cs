

using UKHO.PeriodicOutputService.Common.Providers;

namespace UKHO.PeriodicOutputService.Common.Factories
{
    public interface IHttpClientFactory
    {
        IHttpClientFacade CreateClient(bool enableTimeOut);
    }
}
