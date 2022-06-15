namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public static class CommonHelper
    {
        public static string GetBase64EncodedCredentials(string userName, string password)
        {
            var userCredentialsBytes = System.Text.Encoding.UTF8.GetBytes(userName + ":" + password);
            return Convert.ToBase64String(userCredentialsBytes);
        }

        public static string ExtractAccessToken(string response)
        {
            return response.Split(",")[0].Split(":")[1].Remove(0, 1).Replace("\"", "");
        }
    }
}
