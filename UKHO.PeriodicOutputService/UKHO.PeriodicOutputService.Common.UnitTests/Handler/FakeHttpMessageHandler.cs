using Azure;
using System.Net;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Handler
{
    public class FakeHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpResponseMessage response;

        public static HttpMessageHandler GetHttpMessageHandler(string content, HttpStatusCode httpStatusCode)
        {
            var response = new HttpResponseMessage()
            {
                StatusCode = httpStatusCode,
                Content = new StringContent(content, Encoding.UTF8, "application/json")
            };

            var messageHandler = new FakeHttpMessageHandler(response);

            return messageHandler;
        }

        public FakeHttpMessageHandler(HttpResponseMessage response)
        {
            this.response = response;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();

            tcs.SetResult(response);

            return tcs.Task;
        }
    }
}
