using System.Net;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService.UnitTests.Helpers
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
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientSCSRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new TooManyRequestsDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            result.StatusCode.Should().Be(HttpStatusCode.TooManyRequests);
        }

        [Test]
        public async Task WhenServiceUnavailable_GetRetryPolicy()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientSCSRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new ServiceUnavailableDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");

            result.StatusCode.Should().Be(HttpStatusCode.ServiceUnavailable);
        }

        [Test]
        public async Task WhenInternalServerError_GetRetryPolicy()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHttpClient(TestClient)
                .AddPolicyHandler(CommonHelper.GetRetryPolicy(fakeLogger, "Sales Catalogue", EventIds.RetryHttpClientSCSRequest, RetryCount, SleepDuration))
                .AddHttpMessageHandler(() => new InternalServerErrorDelegatingHandler());

            HttpClient configuredClient =
                services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient(TestClient);

            var result = await configuredClient.GetAsync("https://test.com");
            result.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        }
    }
}
