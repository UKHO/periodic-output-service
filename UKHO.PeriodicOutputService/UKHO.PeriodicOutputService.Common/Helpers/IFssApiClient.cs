namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFssApiClient
    {
        public Task<HttpResponseMessage> CreateBatchAsync(string uri, string requestBody, string authToken);
        public Task<HttpResponseMessage> AddFileToBatchAsync(string uri, string requestBody, string authToken, long? fileContentSizeHeader, string mimeTypeHeader);
        public Task<HttpResponseMessage> UploadFileBlockAsync(string uri, byte[] blockBytes, byte[] md5Hash, string accessToken, string mimeTypeHeader);
        public Task<HttpResponseMessage> WriteBlockInFileAsync(string uri, string requestBody, string accessToken, string mimeTypeHeader);
        public Task<HttpResponseMessage> CommitBatchAsync(string uri, string requestBody, string accessToken);
        public Task<HttpResponseMessage> GetBatchDetailsAsync(string uri, string accessToken);
        public Task<HttpResponseMessage> GetBatchStatusAsync(string uri, string accessToken);
        public Task<HttpResponseMessage> DownloadFile(string uri, string accessToken, string rangeHeader);
        public Task<HttpResponseMessage> DownloadFile(string uri, string accessToken);
        Task<HttpResponseMessage> GetAncillaryFileDetailsAsync(string uri, string accessToken);
    }
}
