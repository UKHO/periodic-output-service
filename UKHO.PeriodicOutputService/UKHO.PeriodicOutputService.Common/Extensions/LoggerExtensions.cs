using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Logging;

namespace UKHO.PeriodicOutputService.Common.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class LoggerExtensions
    {
        public static R LogStartEndAndElapsedTime<T, R>(this ILogger<T> logger,
            EventIds startEventId,
            EventIds completedEventId,
            string messageFormat,
            Func<R> func,
            params object[] messageArguments)
        {
            logger.LogInformation(startEventId.ToEventId(), messageFormat, messageArguments);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {
                return func();
            }
            finally
            {
                stopwatch.Stop();
                logger.LogInformation(completedEventId.ToEventId(),
                    messageFormat + " Elapsed {Elapsed}",
                    messageArguments.Concat(new object[] { stopwatch.Elapsed }).ToArray());
            }
        }

        public static async Task<TResult> LogStartEndAndElapsedTimeAsync<T, TResult>(this ILogger<T> logger,
        EventIds startEventId,
        EventIds completedEventId,
        string startMessageFormat,
        string completedMessageFormat,
        Func<Task<TResult>> func,
        params object[] messageArguments)
        {
            logger.LogInformation(startEventId.ToEventId(), startMessageFormat, messageArguments);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await func();

            stopwatch.Stop();
            logger.LogInformation(completedEventId.ToEventId(),
                completedMessageFormat + " Elapsed {Elapsed}",
                messageArguments.Concat(new object[] { stopwatch.Elapsed }).ToArray());

            return result;
        }

        public static async Task<TResult> LogStartEndAndElapsedTimeAsync<T, TResult>(this ILogger<T> logger,
        EventIds startEventId,
        EventIds completedEventId,
        string messageFormat,
        Func<Task<TResult>> func,
        params object[] messageArguments)
        {
            logger.LogInformation(startEventId.ToEventId(), messageFormat, messageArguments);

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var result = await func();

            stopwatch.Stop();
            logger.LogInformation(completedEventId.ToEventId(),
                messageFormat + " Elapsed {Elapsed}",
                messageArguments.Concat(new object[] { stopwatch.Elapsed }).ToArray());

            return result;
        }
    }
}
