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
        private WireMockServer? server;
        private readonly ILogger logger;
        private readonly WireMockServerSettings settings;
        public const string PksUrl = "/keys/ENC-S63";
        public const string ContentType = "Content-Type";
        public const string ApplicationType = "application/json";

        private class Logger : IWireMockLogger
        {
            private readonly ILogger logger;

            public Logger(ILogger logger)
            {
                this.logger = logger;
            }

            public void Debug(string formatString, params object[] args) => logger.LogDebug(formatString, args);

            public void Info(string formatString, params object[] args) => logger.LogInformation(formatString, args);

            public void Warn(string formatString, params object[] args) => logger.LogWarning(formatString, args);

            public void Error(string formatString, params object[] args) => logger.LogError(formatString, args);

            public void DebugRequestResponse(LogEntryModel logEntryModel, bool isAdminRequest)
            {
                string message = JsonConvert.SerializeObject(logEntryModel, Formatting.Indented);
                logger.LogDebug("Admin[{0}] {1}", isAdminRequest, message);
            }

            public void Error(string formatString, Exception exception) => logger.LogError(formatString, exception.Message);
        }

        public WireMockService(ILogger<WireMockService> logger, IOptions<WireMockServerSettings> settings)
        {
            this.logger = logger;
            this.settings = settings.Value;

            this.settings.Logger = new Logger(logger);
        }

        public void Start()
        {
            logger.LogInformation("WireMock.Net server starting");

            server = WireMockServer.Start(settings);

            server.Given(Request.Create().WithPath(PksUrl)
                        .WithBody(string.Empty)
                        .UsingPost())
                    .RespondWith(Response.Create().WithStatusCode(415)
                        .WithHeader(ContentType, ApplicationType)
                        .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "blankResponse.json")));

            server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request1.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader(ContentType, ApplicationType)
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response1.json")));

            server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request2.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(400)
                    .WithHeader(ContentType, ApplicationType)
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response2.json")));

            server.Given(Request.Create().WithPath(PksUrl)
                   .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request4.json"))))
                   .UsingPost())
               .RespondWith(Response.Create().WithStatusCode(400)
                   .WithHeader(ContentType, ApplicationType)
                   .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response4.json")));

            // actual representation of permit, can be decrypted with dummy machine hardware key - 7E,A1,85,6E,2A
            server.Given(Request.Create().WithPath(PksUrl)
                   .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request6.json"))))
                   .UsingPost())
               .RespondWith(Response.Create().WithStatusCode(200)
                   .WithHeader(ContentType, ApplicationType)
                   .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response6.json")));

            // actual representation of permit, can be decrypted with dummy machine hardware key - 7E,A1,85,6E,2A
            server.Given(Request.Create().WithPath(PksUrl)
                    .WithBody(new JsonMatcher(GetJsonData(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Request5.json"))))
                    .UsingPost())
                .RespondWith(Response.Create().WithStatusCode(200)
                    .WithHeader(ContentType, ApplicationType)
                    .WithBodyFromFile(Path.Combine(Path.GetFullPath(Path.Combine(Environment.CurrentDirectory)), "__files", "Response5.json")));

            logger.LogInformation($"WireMock.Net server settings {JsonConvert.SerializeObject(settings)}");
        }

        private static string GetJsonData(string filePath)
        {
            using var fileStream = new StreamReader(filePath);
            return fileStream.ReadToEnd();
        }

        public void Stop()
        {
            logger.LogInformation("WireMock.Net server stopping");
            server?.Stop();
        }
    }
}