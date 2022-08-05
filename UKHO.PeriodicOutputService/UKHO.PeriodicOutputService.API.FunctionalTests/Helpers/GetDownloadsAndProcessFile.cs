namespace UKHO.PeriodicOutputService.API.FunctionalTests.Helpers
{
    public class GetDownloadsAndProcessFile
    {
        private readonly GetHttpResponseMessage _getHttpResponseMessage = new();

        public async Task<Stream> DownloadFile(string baseURL, string fileLink, string accessToken)
        {
            string fileUri = $"{baseURL}" + fileLink;

            HttpResponseMessage fileDownloadResponse = await _getHttpResponseMessage.GetHttpResponse(fileUri, accessToken);

            return await fileDownloadResponse.Content.ReadAsStreamAsync();
        }

        public byte[] ConvertStreamToByteArray(Stream input)
        {
            var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
        }

        public void CreateFileCopy(string filePath, Stream stream)
        {
            if (stream != null)
            {
                using (var outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.CopyTo(outputFileStream);
                }
            }
        }
    }
}
