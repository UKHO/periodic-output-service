using System.Net;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Enums;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    public class CommonHelperTests
    {
        private ILogger<SalesCatalogueService> _fakeLogger;
        private const int RetryCount = 3;
        private const double SleepDuration = 2;
        private const string TestClient = "TestClient";

        [SetUp]
        public void Setup()
        {
            _fakeLogger = A.Fake<ILogger<SalesCatalogueService>>();
        }

        [Test]
        public async Task WhenTooManyRequests_GetRetryPolicy()
        {
            var services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(_fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientScsRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            var configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.TooManyRequests));

            A.CallTo(_fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            var services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(_fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientScsRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            var configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.ServiceUnavailable));

            A.CallTo(_fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [Test]
        public async Task WhenInternalServerError_GetRetryPolicy()
        {
            var services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(_fakeLogger, "File Share", EventIds.RetryHttpClientScsRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new InternalServerErrorDelegatingHandler());

            var configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");
            
            Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            A.CallTo(_fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.RetryHttpClientScsRequest.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Re-trying {requestType} service request with uri {RequestUri} and delay {delay}ms and retry attempt {retry} with _X-Correlation-ID:{correlationId} as previous request was responded with {StatusCode}."
                  ).MustHaveHappenedTwiceOrMore();
        }

        [TestCase(2025, 12, 24, new string[] { "51", "52", "01", "02" }, new string[] { "52", "01", "02", "03" }, Description = "2025 has 52 weeks. 2025-12-24 is a Wednesday.", TestName = $"{nameof(GetCurrentWeekNumber_For)}_2025/26")]
        [TestCase(2026, 12, 23, new string[] { "51", "52", "53", "01" }, new string[] { "52", "53", "01", "02" }, Description = "2026 has 53 weeks. 2025-12-23 is a Wednesday.", TestName = $"{nameof(GetCurrentWeekNumber_For)}_2026/27")]
        public void GetCurrentWeekNumber_For(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu)
        {
            CheckGetCurrentWeekNumberCommon(startYear, startMonth, startDay, expectedWeekNumberWed, expectedWeekNumberThu, CommonHelper.GetCurrentWeekNumber);
        }

        [TestCase(2025, 12, 24, new string[] { "52", "01", "02", "03" }, new string[] { "01", "02", "03", "04" }, Description = "2025 has 52 weeks. 2025-12-24 is a Wednesday.", TestName = $"{nameof(GetCurrentWeekNumber_AfterIncrementingWeeks_For)}_2025/26")]
        [TestCase(2026, 12, 23, new string[] { "52", "53", "01", "02" }, new string[] { "53", "01", "02", "03" }, Description = "2026 has 53 weeks. 2025-12-23 is a Wednesday.", TestName = $"{nameof(GetCurrentWeekNumber_AfterIncrementingWeeks_For)}_2026/27")]
        public void GetCurrentWeekNumber_AfterIncrementingWeeks_For(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu)
        {
            CheckGetCurrentWeekNumberCommon(startYear, startMonth, startDay, expectedWeekNumberWed, expectedWeekNumberThu, (x) => CommonHelper.GetCurrentWeekNumber(x, 1));
        }

        private static void CheckGetCurrentWeekNumberCommon(int startYear, int startMonth, int startDay, string[] expectedWeekNumberWed, string[] expectedWeekNumberThu, Func<DateTime, FormattedWeekNumber> getCurrentWeekNumber)
        {
            Assert.Multiple(() =>
            {
                Assert.That(expectedWeekNumberWed, Has.Length.EqualTo(4), "The test covers a four week period so there must be four expected results.");
                Assert.That(expectedWeekNumberThu, Has.Length.EqualTo(4), "The test covers a four week period so there must be four expected results.");
            });

            var checkDateWed = new DateTime(startYear, startMonth, startDay, 8, 0, 0, DateTimeKind.Utc);
            var checkDateThu = checkDateWed.AddDays(1);

            for (var i = 0; i < 3; i++)
            {
                var weekNumberWed = getCurrentWeekNumber(checkDateWed);
                var weekNumberThu = getCurrentWeekNumber(checkDateThu);

                Assert.Multiple(() =>
                {
                    Assert.That(weekNumberWed.Week, Is.EqualTo(expectedWeekNumberWed[i]));
                    Assert.That(weekNumberThu.Week, Is.EqualTo(expectedWeekNumberThu[i]));
                });

                checkDateWed = checkDateWed.AddDays(7);
                checkDateThu = checkDateThu.AddDays(7);
            }
        }

        [TestCase(Batch.BessBaseZipBatch, false, TestName = $"{nameof(IsAio_BatchType)}_{nameof(Batch.BessBaseZipBatch)}")]
        [TestCase(Batch.AioBaseCDZipIsoSha1Batch, true, TestName = $"{nameof(IsAio_BatchType)}_{nameof(Batch.AioBaseCDZipIsoSha1Batch)}")]
        [TestCase(Batch.EssAioBaseZipBatch, false, TestName = $"{nameof(IsAio_BatchType)}_{nameof(Batch.EssAioBaseZipBatch)}")]
        [TestCase(Batch.AioUpdateZipBatch, true, TestName = $"{nameof(IsAio_BatchType)}_{nameof(Batch.AioUpdateZipBatch)}")]
        public void IsAio_BatchType(Batch batchType, bool expectedResult)
        {
            Assert.That(batchType.IsAio(), Is.EqualTo(expectedResult));
        }

        [Test]
        public void CheckMethodReturns_CorrectBase64EncodedCredentials()
        {
            var user1Credentials = CommonHelper.GetBase64EncodedCredentials("User1", "Password1");
            Assert.That(user1Credentials, Is.EqualTo("VXNlcjE6UGFzc3dvcmQx"));
        }

        [Test]
        public void CheckMethodReturns_CorrectExtractAccessToken()
        {
            var extractedAccessToken = CommonHelper.ExtractAccessToken("{\"token\":\"eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk1234.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234\",\"expiration\":\"2022-06-15T16:02:52Z\"}");
            Assert.That(extractedAccessToken, Is.EqualTo("eyJhbGciOiJIUzI1NiIsInR5cCI6I1234212CJ9.VLSE9fRk1234.fd73LguLf_6VBefVQqu0nj8j3dovfUNVeqZDYGZ1234"));
        }
    }
}
