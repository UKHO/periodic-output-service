namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class FssApiClient
    {
        static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Get Batch Status
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="accessToken">Access Token, pass NULL to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken = null)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }


        /// <summary>
        /// get file download
        /// </summary>
        /// <param name="uri"></param>
        /// <param name="filerangeheader">file range header, pass null to skip partial download</param>
        /// <param name="accesstoken">access token, pass null to skip auth header</param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> GetFileDownloadAsync(string uri, string filerangeheader = null, string accessToken = null)
        {

            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, uri))
            {
                if (filerangeheader != null)
                {
                    httpRequestMessage.Headers.Add("range", filerangeheader);
                }

                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }
    }
}
