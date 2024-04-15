namespace UKHO.BESS.API.FunctionalTests.Helpers
{
    public class FssEndPointHelper
    {
        static readonly HttpClient httpClient = new();

        /// <summary>
        /// This method is used to get the batch status endpoint response.
        /// </summary>
        /// <param name="uri"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        /// <summary>
        /// This method is used to get the file download endpoint response
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="fileRangeHeader"></param>
        /// <returns></returns>
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
