using Azure;
using System.Net;
using System.Text;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Handler
{
    public class FakeHttpMessageHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        private readonly HttpResponseMessage _response = response;

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

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<HttpResponseMessage>();

            tcs.SetResult(_response);

            return tcs.Task;
        }
    }
}
