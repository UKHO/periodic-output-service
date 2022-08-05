using Newtonsoft.Json;
using UKHO.PeriodicOutputService.API.FunctionalTests.Enums;
using UKHO.PeriodicOutputService.API.FunctionalTests.Models;

namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetBatchElements
    {
        private readonly GetHttpResponseMessage _getHttpResponseMessage = new();

        public async Task<FssBatchStatus> CheckIfBatchCommitted(string baseURL, string batchId, string accessToken, string BatchStatusPollingCutoffTime, string BatchStatusPollingDelayTime)
        {
            FssBatchStatus batchStatus = FssBatchStatus.Incomplete;
            DateTime startTime = DateTime.UtcNow;

            string uri = $"{baseURL}/batch/{batchId}status";

            while (DateTime.UtcNow - startTime < TimeSpan.FromMinutes(double.Parse(BatchStatusPollingCutoffTime)))
            {
                await Task.Delay(int.Parse(BatchStatusPollingDelayTime));

                HttpResponseMessage batchStatusResponse = await _getHttpResponseMessage.GetHttpResponse(uri, accessToken);

                if (!batchStatusResponse.IsSuccessStatusCode)
                {
                    break;
                }
                FssBatchStatusResponseModel responseObj = JsonConvert.DeserializeObject<FssBatchStatusResponseModel>(await batchStatusResponse.Content.ReadAsStringAsync());

                Enum.TryParse(responseObj?.Status, false, out batchStatus);

            }
            return batchStatus;
        }

        public async Task<List<FssBatchFile>> GetBatchFiles(string baseURL, string batchId, string accessToken)
        {
            string uri = $"{baseURL}/batch/{batchId}";

            HttpResponseMessage batchDetailResponse = await _getHttpResponseMessage.GetHttpResponse(uri, accessToken);

            GetBatchResponseModel batchDetail = JsonConvert.DeserializeObject<GetBatchResponseModel>(await batchDetailResponse.Content.ReadAsStringAsync());

            List<FssBatchFile> batchFiles = null;

            if (batchDetail != null)
            {
                batchFiles = batchDetail.Files.Select(a => new FssBatchFile { FileName = a.Filename, FileLink = a.Links.Get.Href }).ToList();

                if (batchFiles.Any(f => f.FileName.ToLower().Contains("error")))
                {
                    return null;
                }
            }
            return batchFiles;
        }
    }
}
