using System.Net;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    public class CommonHelperTests
    {
        private ILogger<SalesCatalogueService> fakeLogger;
        private const int RetryCount = 3;
        private const double SleepDuration = 2;
        private const string TestClient = "TestClient";

        [SetUp]
        public void Setup()
        {
            fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientScsRequest , RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest .ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientScsRequest , RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest .ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [Test]
        public async Task WhenInternalServerError_GetRetryPolicy()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "File Share", EventIds.RetryHttpClientScsRequest , RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new InternalServerErrorDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest .ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [TestCase(2025, 12, 24, new string[] { "51", "52", "01", "02" }, new string[] { "52", "01", "02", "03" }, Description = "2025 has 52 weeks. 2025-12-24 is a Wednesday.", TestName = nameof(Check_GetCurrentWeekNumber) + "_2025/26")]
        [TestCase(2026, 12, 23, new string[] { "51", "52", "53", "01" }, new string[] { "52", "53", "01", "02" }, Description = "2026 has 53 weeks. 2025-12-23 is a Wednesday.", TestName = nameof(Check_GetCurrentWeekNumber) + "_2026/27")]
        public void Check_GetCurrentWeekNumber(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu)
        {
            CheckGetCurrentWeekNumberCommon(startYear, startMonth, startDay, expectedWeekNumberWed, expectedWeekNumberThu, (x) => CommonHelper.GetCurrentWeekNumber(x));
        }

        [TestCase(2025, 12, 24, new string[] { "52", "01", "02", "03" }, new string[] { "01", "02", "03", "04" }, Description = "2025 has 52 weeks. 2025-12-24 is a Wednesday.", TestName = nameof(Check_GetCurrentWeekNumber_AfterIncrementingWeeks) + "_2025/26")]
        [TestCase(2026, 12, 23, new string[] { "52", "53", "01", "02" }, new string[] { "53", "01", "02", "03" }, Description = "2026 has 53 weeks. 2025-12-23 is a Wednesday.", TestName = nameof(Check_GetCurrentWeekNumber_AfterIncrementingWeeks) + "_2026/27")]
        public void Check_GetCurrentWeekNumber_AfterIncrementingWeeks(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu)
        {
            CheckGetCurrentWeekNumberCommon(startYear, startMonth, startDay, expectedWeekNumberWed, expectedWeekNumberThu, (x) => CommonHelper.GetCurrentWeekNumber(x, 1));
        }

        private static void CheckGetCurrentWeekNumberCommon(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu, Func<DateTime, string> getCurrentWeekNumber)
        {
            Assert.Multiple(() =>
            {
                Assert.That(expectedWeekNumberWed, Has.Length.EqualTo(4), "The test covers a four week period so there must be four expected results.");
                Assert.That(expectedWeekNumberThu, Has.Length.EqualTo(4), "The test covers a four week period so there must be four expected results.");
            });

            var checkDateWed = new DateTime(startYear, startMonth, startDay, 8, 0, 0, DateTimeKind.Utc);
            var checkDateThu = checkDateWed.AddDays(1);

            for (int i = 0; i < 3; i++)
            {
                var weekNumberWed = getCurrentWeekNumber(checkDateWed);
                var weekNumberThu = getCurrentWeekNumber(checkDateThu);

                Assert.Multiple(() =>
                {
                    Assert.That(weekNumberWed, Is.EqualTo(expectedWeekNumberWed[i]));
                    Assert.That(weekNumberThu, Is.EqualTo(expectedWeekNumberThu[i]));
                });

                checkDateWed = checkDateWed.AddDays(7);
                checkDateThu = checkDateThu.AddDays(7);
            }
        }
    }
}
