using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Fulfilment.Configuration;
using UKHO.PeriodicOutputService.Fulfilment.Services;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Services
{
    [TestFixture]
    public class FssBatchServiceTests
    {
        private IOptions<FssApiConfiguration> _fakeFssApiConfiguration;
        private ILogger<FssBatchService> _fakeLogger;
        private IFssApiClient _fakeFssApiClient;
        private IAuthFssTokenProvider _fakeAuthFssTokenProvider;
        private IFssBatchService _fakeBatchService;

        [SetUp]
        public void Setup()
        {
            _fakeFssApiConfiguration = Options.Create(new FssApiConfiguration() { BaseUrl = "http://test.com",
                                                                                  FssClientId = "8YFGEFI78TYIUGH78YGHR5",
                                                                                  BatchStatusPollingCutoffTime = "1",
                                                                                  BatchStatusPollingDelayTime = "20000"});

            _fakeLogger = A.Fake<ILogger<FssBatchService>>();

            _fakeFssApiClient = A.Fake<IFssApiClient>();

            _fakeAuthFssTokenProvider = A.Fake<IAuthFssTokenProvider>();

            _fakeBatchService = new FssBatchService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
                () => new FssBatchService(null, _fakeFssApiConfiguration, _fakeFssApiClient, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("logger");

            Assert.Throws<ArgumentNullException>(
                () => new FssBatchService(_fakeLogger, null, _fakeFssApiClient, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("fssApiConfiguration");

            Assert.Throws<ArgumentNullException>(
                () => new FssBatchService(_fakeLogger, _fakeFssApiConfiguration, null, _fakeAuthFssTokenProvider))
                .ParamName
                .Should().Be("fssApiClient");

            Assert.Throws<ArgumentNullException>(
                () => new FssBatchService(_fakeLogger, _fakeFssApiConfiguration, _fakeFssApiClient, null))
                .ParamName
                .Should().Be("authFssTokenProvider");
        }

        [Test]
        public async Task DoesCheckIfBatchCommitted_Returns_BatchStatus()
        {
            A.CallTo(() => _fakeFssApiClient.GetBatchStatusAsync(A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes("{\"batchId\":\"4c5397d5-8a05-43fa-9009-9c38b2007f81\",\"status\":\"Incomplete\"}")))
                });

            var result = await _fakeBatchService.CheckIfBatchCommitted("http://test.com/4c5397d5-8a05-43fa-9009-9c38b2007f81/status");

            Assert.That(result, Is.Not.Null);
            Assert.That(result, Is.Not.Null);

            A.CallTo(() => _fakeAuthFssTokenProvider.GetManagedIdentityAuthAsync(A<string>.Ignored))
                .MustHaveHappenedOnceExactly();

        }
    }
}
