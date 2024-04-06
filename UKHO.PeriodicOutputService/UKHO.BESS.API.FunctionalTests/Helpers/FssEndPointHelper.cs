using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class FssEndPointHelper
    {
        static readonly HttpClient httpClient = new();

        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public async Task<HttpResponseMessage> GetFileDownloadAsync(string uri, string? fileRangeHeader = null)
        {

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (fileRangeHeader != null)
                {
                    httpRequestMessage.Headers.Add("Range", fileRangeHeader);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);

            }
        }
    }
}
