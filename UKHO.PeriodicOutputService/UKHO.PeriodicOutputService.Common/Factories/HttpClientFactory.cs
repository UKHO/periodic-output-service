using System.Diagnostics.CodeAnalysis;
using UKHO.PeriodicOutputService.Common.Providers;

namespace UKHO.PeriodicOutputService.Common.Factories
{
    [ExcludeFromCodeCoverage]
    public class HttpClientFactory : IHttpClientFactory
    {
        private IHttpClientFacade _client;

        public IHttpClientFacade CreateClient(bool enableTimeOut)
        {
            return _client ??= new HttpClientFacade(new HttpClient(), enableTimeOut);
        }
    }
}
