using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using WireMock.Admin.Requests;
using WireMock.Logging;
using WireMock.Matchers;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;
using WireMock.Settings;

namespace UKHO.PKSWireMock.API
{
    public class WireMockService : IWireMockService
    {
        private WireMockServer? _server;
        private readonly ILogger _logger;
        private readonly WireMockServerSettings _settings;
        public const string PksUrl = "/keys/ENC-S63";
        private class Logger : IWireMockLogger
        {
            private readonly ILogger _logger;

            public Logger(ILogger logger)
            {
                _logger = logger;
            }

            public void Debug(string formatString, params object[] args) => _logger.LogDebug(formatString, args);

            public void Info(string formatString, params object[] args) => _logger.LogInformation(formatString, args);

            public void Warn(string formatString, params object[] args) => _logger.LogWarning(formatString, args);

            public void Error(string formatString, params object[] args) => _logger.LogError(formatString, args);

            public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminrequest)
            {
                string message = JsonConvert.SerializeObject(logEntryModel, Formatting.Indented);
                _logger.LogDebug("Admin[{0}] {1}", isAdminrequest, message);
            }

            public void Error(string formatString, Exception exception) => _logger.LogError(formatString, exception.Message);
        }

        public WireMockService(ILogger<WireMockService> logger, IOptions<WireMockServerSettings> settings)
        {
            _logger = logger;
            _settings = settings.Value;

            _settings.Logger = new Logger(logger);
        }

        public void Start()
        {
            _logger.LogInformation("WireMock.Net server starting");

            _server = WireMockServer.Start(_settings);

            _server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request1.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response1.json")));

            _server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request2.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(400)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response2.json")));

            _server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(string.Empty)
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(415)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "blankResponse.json")));

            _server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request3.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response3.json")));

            _server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request4.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(400)
                    .WithHeader("Content-Type", "application/json")
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response4.json")));

            _logger.LogInformation($"WireMock.Net server settings {JsonConvert.SerializeObject(_settings)}");
        }

        private static string GetJsonData(string filePath)
        {            
            using (var r = new StreamReader(filePath))
            {
                string jsonFile = r.ReadToEnd();
                return jsonFile;
            }
        }

        public void Stop()
        {
            _logger.LogInformation("WireMock.Net server stopping");
            _server?.Stop();
        }
    }
}
