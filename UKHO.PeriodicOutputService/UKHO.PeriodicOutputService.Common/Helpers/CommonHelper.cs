using System.Globalization;
using System.Security.Cryptography;

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

        public static string GetBlockIds(int blockNum)
        {
            string blockId = $"Block_{blockNum:00000}";
            return blockId;
        }

        public static byte[] CalculateMD5(byte[] requestBytes)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestBytes);

            return hash;
        }

        public static byte[] CalculateMD5(Stream requestStream)
        {
            using var md5 = MD5.Create();
            var hash = md5.ComputeHash(requestStream);

            return hash;
        }

        public static int GetCurrentWeekNumber(DateTime date)
        {
            CultureInfo cultureInfo = CultureInfo.InvariantCulture;
            return cultureInfo.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday);
        }
    }
}
