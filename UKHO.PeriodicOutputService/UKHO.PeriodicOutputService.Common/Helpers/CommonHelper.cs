namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public static class CommonHelper
    {
        public static Guid CorrelationID { get; set; } = Guid.NewGuid();

        public static string GetBase64EncodedCredentials(string userName, string password)
        {
            byte[]? userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }

        public static string ExtractAccessToken(string response) => response.Split(",")[0].Split(":")[1].Remove(0, 1).Replace("\"", "");

        public static string ExtractBatchId(string url) => new UriBuilder(url).Uri.Segments.FirstOrDefault(d => Guid.TryParse(d.Replace("/", ""), out Guid _));
    }
}
