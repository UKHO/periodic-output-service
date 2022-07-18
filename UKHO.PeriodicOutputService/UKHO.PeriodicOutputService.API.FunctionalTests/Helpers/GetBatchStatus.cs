using Newtonsoft.Json;
using UKHO.PeriodicOutputService.API.FunctionalTests.Enums;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetBatchStatus
    {
        private static readonly HttpClient httpClient = new();

        private async Task<HttpResponseMessage> GetBatchStatusAsync(string url, string accessToken)
        {
            using (var httpRequestMessage = new HttpRequestMessage(HttpMethod.Get, url))
            {
                if (accessToken != null)
                {
                    httpRequestMessage.SetBearerToken(accessToken);
                }

                return await httpClient.SendAsync(httpRequestMessage, CancellationToken.None);
            }
        }

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string baseURL, string batchStatusURI, string accessToken, string BatchStatusPollingCutoffTime, string BatchStatusPollingDelayTime)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string batchId = CommonHelper.ExtractBatchId(batchStatusURI);

            string uri = $"{baseURL}/batch/{batchId}status";

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(BatchStatusPollingCutoffTime)))
            {
                await Task.Delay(int.Parse(BatchStatusPollingDelayTime));

                HttpResponseMessage batchStatusResponse = await GetBatchStatusAsync(uri, accessToken);

                if (!batchStatusResponse.IsSuccessStatusCode)
                {
                    break;
                }
                FssBatchStatusResponseModel responseObj = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());

                Enum.TryParse(responseObj?.Status, false, out batchStatus);

            }
            return batchStatus;
        }
    }
}
