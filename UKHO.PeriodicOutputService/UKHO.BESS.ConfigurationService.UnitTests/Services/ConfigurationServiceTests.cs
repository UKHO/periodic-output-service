using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.BESS.ConfigurationService.UnitTests.Services
{
    [TestFixture]
    public class ConfigurationServiceTests
    {
        private IConfigurationService configurationService;
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;
        private ILogger<ConfigurationService.Services.ConfigurationService> fakeLogger;
        private const string InvalidConfigJson = "[{\"Name\":,\"ExchangeSetStandard\":null,\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":true}]";
        private const string ValidConfigJson = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":true}]";
        private const string InvalidEmptyJson = "[{,,,}]";
        private const string DuplicateConfigJson = "[{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":true}," +
                                                   "{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":true}]";
        private Dictionary<string, string> dictionary;
        private IConfigValidator fakeConfigValidator;

        [SetUp]
        public void SetUp()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeLogger = A.Fake<ILogger<ConfigurationService.Services.ConfigurationService>>();
            fakeConfigValidator = A.Fake<IConfigValidator>();

            configurationService = new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeLogger, fakeConfigValidator);
            dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureBlobStorageClient = () => new ConfigurationService.Services.ConfigurationService(null, fakeLogger, fakeConfigValidator);

            nullAzureBlobStorageClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageClient");

            Action nullConfigurationServiceLogger = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, null, fakeConfigValidator);

            nullConfigurationServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullConfigValidator = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeLogger, null);

            nullConfigValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configValidator");
        }

        [Test]
        public void WhenValidConfigIsFound_ThenConfigIsAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetValidConfigFilesJson());
            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidConfigIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetInvalidEmptyJson());
            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing Bess config file:{fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenUndefinedValuesFoundInConfig_ThenConfigIsNotAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetInvalidConfigFilesJson());
            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                 && call.GetArgument<EventId>(1) == EventIds.BessConfigIsInvalid.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess config is invalid for file : {fileName} | _X-Correlation-ID : {CorrelationId}"
                 ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenGetConfigsInContainerMethodSendNull_ThenThrowsException()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Throws<Exception>();
            configurationService.Invoking(x => x.ProcessConfigs()).Should().ThrowExactly<Exception>();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingFailed.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs Processing failed with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenContainerHasNoConfigs_ThenConfigIsNotAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(new Dictionary<string, string>());
            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsNotFound.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs not found | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenDuplicateConfigIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetDuplicateConfigJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));

            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs count:{count}  | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            //A.CallTo(fakeLogger).Where(call =>
            //    call.Method.Name == "Log"
            //    && call.GetArgument<LogLevel>(0) == LogLevel.Error
            //    && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
            //    && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing Bess config file:{fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
            //).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        private Dictionary<string, string> GetValidConfigFilesJson()
        {
            dictionary.Add("Valid.json", ValidConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetInvalidConfigFilesJson()
        {
            dictionary.Add("Invalid.json", InvalidConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetInvalidEmptyJson()
        {
            dictionary.Add("Empty.json", InvalidEmptyJson);
            return dictionary;
        }

        private Dictionary<string, string> GetDuplicateConfigJson()
        {
            dictionary.Add("DuplicateConfigName.json", DuplicateConfigJson);
            return dictionary;
        }
    }
}
