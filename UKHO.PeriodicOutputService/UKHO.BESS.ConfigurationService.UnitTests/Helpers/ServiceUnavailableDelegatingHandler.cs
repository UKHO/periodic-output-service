using System.Net;

namespace UKHO.BESS.ConfigurationService.UnitTests.Helpers
{
    public class ServiceUnavailableDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();
            httpResponse.RequestMessage = new HttpRequestMessage();
            httpResponse.StatusCode = HttpStatusCode.ServiceUnavailable;
            return Task.FromResult(httpResponse);
        }
    }
}
