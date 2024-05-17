using System.Globalization;
using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Polly;
using UKHO.PeriodicOutputService.Common.Logging;

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

        public static string GetBlockIds(int blockNum) => $"Block_{blockNum:00000}";

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

        public static string GetCurrentWeekNumber(DateTime date) { string currentWeek = CultureInfo.InvariantCulture.Calendar.GetWeekOfYear(date, CalendarWeekRule.FirstFullWeek, DayOfWeek.Thursday).ToString(); return currentWeek.Length == 1 ? string.Concat("0", currentWeek) : currentWeek; }

        public static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy(ILogger logger, string requestType, EventIds eventId, int retryCount, double sleepDuration)
        {
            return Policy
                .HandleResult<HttpResponseMessage>(r => r.StatusCode == HttpStatusCode.ServiceUnavailable)
                .OrResult(r => r.StatusCode == HttpStatusCode.TooManyRequests)
                .OrResult(r => r.StatusCode == HttpStatusCode.InternalServerError && requestType == "File Share")
                .WaitAndRetryAsync(retryCount, (retryAttempt) =>
                {
                    return TimeSpan.FromSeconds(Math.Pow(sleepDuration, (retryAttempt - 1)));
                }, async (response, timespan, retryAttempt, context) =>
                {
                    var retryAfterHeader = response.Result.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "retry-after");
                    var correlationId = response.Result.RequestMessage!.Headers.FirstOrDefault(h => h.Key.ToLowerInvariant() == "x-correlation-id");
                    int retryAfter = 0;
                    if (response.Result.StatusCode == HttpStatusCode.TooManyRequests && retryAfterHeader.Value != null && retryAfterHeader.Value.Any())
                    {
                        retryAfter = int.Parse(retryAfterHeader.Value.First());
                        await Task.Delay(TimeSpan.FromMilliseconds(retryAfter));
                    }
                    logger
                    .LogInformation(eventId.ToEventId(), "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}.",
                    requestType, response.Result.RequestMessage.RequestUri, timespan.Add(TimeSpan.FromMilliseconds(retryAfter)).TotalMilliseconds, retryAttempt, correlationId.Value, response.Result.StatusCode);
                });
        }

        public static double ConvertBytesToMegabytes(long bytes)
        {
            double byteSize = 1024f;
            return (bytes / byteSize) / byteSize;
        }

        public static Dictionary<string, string> MimeTypeList()
        {
            Dictionary<string, string> mimeTypes = new()
            {
                { ".zip", "application/zip" },
                { ".xml", "text/xml" },
                { ".csv", "text/csv" },
                { ".txt", "text/plain" }
            };
            return mimeTypes;
        }
    }
}
