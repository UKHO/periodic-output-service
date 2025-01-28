using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using Polly;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public static class CommonHelper
    {
        public static Guid CorrelationID { get; set; } = Guid.NewGuid();

        public static string GetCorrelationId(string? correlationId)
        {
            return string.IsNullOrEmpty(correlationId) ? CorrelationID.ToString() : correlationId;
        }

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

        /// <summary>
        /// Get the current week number of the year for the given date, based on the standard UKHO week starting on a Thursday.
        /// </summary>
        /// <param name="date"></param>
        /// <returns></returns>
        public static FormattedWeekNumber GetCurrentWeekNumber(DateTime date) => new(WeekNumber.GetUKHOWeekFromDateTime(date));

        /// <summary>
        /// Get the current week number of the year for the given date, incremented by the specified number of weeks.
        /// </summary>
        /// <param name="date"></param>
        /// <param name="weeksToIncrement"></param>
        /// <returns></returns>
        public static FormattedWeekNumber GetCurrentWeekNumber(DateTime date, int weeksToIncrement) => GetCurrentWeekNumber(date.AddDays(7 * weeksToIncrement));

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

        /// <summary>
        /// Check if the batch type is AIO.
        /// </summary>
        /// <param name="batchType"></param>
        /// <returns></returns>
        public static bool IsAioBatchType(this Batch batchType) => batchType == Batch.AioBaseCDZipIsoSha1Batch || batchType == Batch.AioUpdateZipBatch;
    }
}
