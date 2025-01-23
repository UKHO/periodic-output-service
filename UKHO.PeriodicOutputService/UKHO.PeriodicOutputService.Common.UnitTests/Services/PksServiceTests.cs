using System.Net;
using System.Text;
using FakeItEasy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using UKHO.PeriodicOutputService.Common.Configuration;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Services
{
    [TestFixture]
    public class PksServiceTests
    {
        private ILogger<PksService> fakeLogger;
        private IOptions<PksApiConfiguration> fakePksApiConfiguration;
        private IAuthPksTokenProvider fakeAuthPksTokenProvider;
        private IPksApiClient fakePksApiClient;
        private IPksService pksService;

        private const string BadRequestError = "{\"correlationId\":\"abc578cc-0641-4407-ae72-e053ea28b0d8\",\"errors\":[{\"source\":\"GetProductKey\",\"description\":\"Key not found for ProductName: G0123456 and Edition: 9.\"}]}";

        [SetUp]
        public void Setup()
        {
            fakePksApiConfiguration = Options.Create(new PksApiConfiguration() { ClientId = "ClientId2" });
            fakePksApiClient = A.Fake<IPksApiClient>();
            fakeAuthPksTokenProvider = A.Fake<IAuthPksTokenProvider>();
            fakeLogger = A.Fake<ILogger<PksService>>();

            pksService = new PksService(fakeLogger, fakePksApiConfiguration, fakeAuthPksTokenProvider, fakePksApiClient);
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullPksLogger = () => new PksService(null, fakePksApiConfiguration, fakeAuthPksTokenProvider, fakePksApiClient);
            var exception = Assert.Throws<ArgumentNullException>(() => nullPksLogger());
            Assert.That(exception.ParamName, Is.EqualTo("logger"));

            Action nullPksApiConfiguration = () => new PksService(fakeLogger, null, fakeAuthPksTokenProvider, fakePksApiClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullPksApiConfiguration());
            Assert.That(exception.ParamName, Is.EqualTo("pksApiConfiguration"));

            Action nullAuthPksTokenProvider = () => new PksService(fakeLogger, fakePksApiConfiguration, null, fakePksApiClient);
            exception = Assert.Throws<ArgumentNullException>(() => nullAuthPksTokenProvider());
            Assert.That(exception.ParamName, Is.EqualTo("authPksTokenProvider"));

            Action nullPksApiClient = () => new PksService(fakeLogger, fakePksApiConfiguration, fakeAuthPksTokenProvider, null);
            exception = Assert.Throws<ArgumentNullException>(() => nullPksApiClient());
            Assert.That(exception.ParamName, Is.EqualTo("pksApiClient"));
        }

        [Test]
        public async Task WhenProductKeyServiceRequestsValidData_ThenReturnProductKeyServiceValidResponse()
        {
            A.CallTo(() => fakePksApiClient.PostPksDataAsync
                    (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.OK,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StringContent(JsonConvert.SerializeObject(GetProductKeyServiceResponse()))
                });

            List<ProductKeyServiceResponse> response = await pksService.PostProductKeyData(GetProductKeyServiceRequest());
            Assert.That(response, Is.Not.Empty);

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post product key data to Product Key Service started | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post product key data to Product Key Service completed | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenProductKeyServiceRequestsInvalidData_ThenReturnsFulfilmentException()
        {
            A.CallTo(() => fakePksApiClient.PostPksDataAsync
                    (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = HttpStatusCode.BadRequest,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StringContent(BadRequestError)
                });

            var exception = Assert.ThrowsAsync<FulfilmentException>(() => pksService.PostProductKeyData(new List<ProductKeyServiceRequest>()));
            Assert.That(EventIds.PostProductKeyDataToPksFailed.ToEventId(), Is.EqualTo(exception.EventId));

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post product key data to Product Key Service started | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to retrieve post product key data with | StatusCode : {StatusCode}| Errors : {ErrorDetails} for Product Key Service | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        [TestCase(HttpStatusCode.Unauthorized, "Unauthorized")]
        [TestCase(HttpStatusCode.InternalServerError, "InternalServerError")]
        [TestCase(HttpStatusCode.ServiceUnavailable, "ServiceUnavailable")]
        [TestCase(HttpStatusCode.UnsupportedMediaType, "UnsupportedMediaType")]
        public void WhenProductKeyServiceResponseOtherThanOkAndBadRequest_ThenReturnsFulfilmentException(HttpStatusCode statusCode, string content)
        {
            A.CallTo(() => fakePksApiClient.PostPksDataAsync
                    (A<string>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                .Returns(new HttpResponseMessage()
                {
                    StatusCode = statusCode,
                    RequestMessage = new HttpRequestMessage()
                    {
                        RequestUri = new Uri("http://test.com")
                    },
                    Content = new StreamContent(new MemoryStream(Encoding.UTF8.GetBytes(content)))
                });

            var exception = Assert.ThrowsAsync<FulfilmentException>(() => pksService.PostProductKeyData(new List<ProductKeyServiceRequest>()));
            Assert.That(exception.EventId, Is.EqualTo(EventIds.PostProductKeyDataToPksFailed.ToEventId()));

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Request to post product key data to Product Key Service started | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.PostProductKeyDataToPksFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Failed to post product key data | StatusCode : {StatusCode} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        private static List<ProductKeyServiceRequest> GetProductKeyServiceRequest() =>
            new()
            {
                new()
                {
                    ProductName = "D0123456",
                    Edition = "1"
                }
            };

        private static List<ProductKeyServiceResponse> GetProductKeyServiceResponse()
        {
            return new List<ProductKeyServiceResponse>
            {
                new() { ProductName = "D0123456", Edition = "1", Key = "test" }
            };
        }
    }
}
