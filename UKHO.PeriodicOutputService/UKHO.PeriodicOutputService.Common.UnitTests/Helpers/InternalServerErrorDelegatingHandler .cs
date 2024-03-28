using System.Net;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    public class InternalServerErrorDelegatingHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var httpResponse = new HttpResponseMessage();
            httpResponse.RequestMessage = new HttpRequestMessage();
            httpResponse.StatusCode = HttpStatusCode.InternalServerError;
            return Task.FromResult(httpResponse);
        }
    }
}
