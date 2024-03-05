using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.UnitTests.Services
{
    [TestFixture]
    public class ConfigurationServiceTests
    {
        private IConfigurationService configurationService;
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;
        private ILogger<ConfigurationService.Services.ConfigurationService> fakeLogger;
        private const string InvalidConfigJson = "[{\"Name\":,\"ExchangeSetStandard\":null,\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"no\"}]";

        private const string ValidConfigJson = "[{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}," +
                                               "{\"Name\":\"Abc\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}]";

        private const string InvalidEmptyJson = "[{,,,}]";

        private const string DuplicateConfigJson = "[{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}," +
                                                   "{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";

        private const string ConfigJsonWithIncorrectExchangeSetStandard = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";
        private Dictionary<string, string> dictionary;
        private IConfigValidator fakeConfigValidator;

        [SetUp]
        public void SetUp()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakeLogger = A.Fake<ILogger<ConfigurationService.Services.ConfigurationService>>();
            fakeConfigValidator = A.Fake<IConfigValidator>();

            configurationService = new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator);
            dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureBlobStorageClient = () => new ConfigurationService.Services.ConfigurationService(null, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator);

            nullAzureBlobStorageClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageClient");

            Action nullAzureTableHelper = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, null, fakeLogger, fakeConfigValidator);

            nullAzureTableHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullConfigurationServiceLogger = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, null, fakeConfigValidator);

            nullConfigurationServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullConfigValidator = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, null);

            nullConfigValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configValidator");
        }

        [Test]
        public void WhenValidConfigIsFound_ThenConfigIsAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetValidConfigFilesJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();
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
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing Bess config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
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
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                 && call.GetArgument<EventId>(1) == EventIds.BessConfigIsInvalid.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess config file : {fileName} contains undefined values or is invalid | _X-Correlation-ID : {CorrelationId}"
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
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing failed with Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
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
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
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
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsDuplicateRecordsFound.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess config file : {fileName} found with duplicate Name attribute : {name} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with undefined value : {filesWithUndefinedValueCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess configs processing completed | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenIncorrectExchangeSetStandardIsFound_ThenThrowsValidationError()
        {
            var validationMessage = new ValidationFailure("ExchangeSetStandard", "Attribute value is invalid. Expected value is either s63 or s57");
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetConfigJsonWithIncorrectExchangeSetStandard());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigInvalidAttributes.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Bess Config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappened();
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

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessful_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsThrowsException_ThenReturnsFalse()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Throws<Exception>();

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Exception occurred while processing Bess config {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            Assert.That(result, Is.False);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsAddedToQueue_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsNotAddedToQueue_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting());

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsNotAddedToQueueSameDay_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueueOnSameDay());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingNotEnabled());

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndWhenNextScheduleDetailsIsNull_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1"))!.Returns(null);

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting());

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        private ScheduleDetailEntity GetFakeScheduleDetailsToAddInQueue()
        {
            var history = new ScheduleDetailEntity
            {
                PartitionKey = "BessConfigSchedule",
                RowKey = "BESS-1",
                NextScheduleTime = DateTime.UtcNow,
                IsEnabled = "Yes",
                IsExecuted = false,
            };
            return history;
        }

        private ScheduleDetailEntity GetFakeScheduleDetailsNotToAddInQueue()
        {
            var history = new ScheduleDetailEntity

            {
                PartitionKey = "BessConfigSchedule",
                RowKey = "BESS-1",
                NextScheduleTime = DateTime.UtcNow.AddDays(1),
                IsEnabled = "No",
                IsExecuted = true
            };
            return history;
        }

        private ScheduleDetailEntity GetFakeScheduleDetailsNotToAddInQueueOnSameDay()
        {
            var history = new ScheduleDetailEntity

            {
                PartitionKey = "BessConfigSchedule",
                RowKey = "BESS-1",
                NextScheduleTime = DateTime.UtcNow,
                IsEnabled = "Yes",
                IsExecuted = false
            };
            return history;
        }

        private List<BessConfig> GetFakeConfigurationSetting()
        {
            int todayDay = DateTime.UtcNow.Day;
            List<BessConfig> configurations = new()
        {
            new()
            {
                Name = "BESS-1",
                ExchangeSetStandard = "s63",
                EncCellNames = new List<string> { "" },
                Frequency = $"0 * {todayDay} * *",
                Type = "",
                KeyFileType = "",
                AllowedUsers = new List<string>(),
                AllowedUserGroups = new List<string>(),
                Tags = new List<Tag>(),
                ReadMeSearchFilter = "",
                BatchExpiryInDays = 1,
                IsEnabled = "Yes"
            },
        };
            return configurations;
        }

        private List<BessConfig> GetFakeConfigurationSettingNotEnabled()
        {
            int todayDay = DateTime.UtcNow.Day;
            List<BessConfig> configurations = new()
        {
            new()
            {
                Name = "BESS-1",
                ExchangeSetStandard = "s63",
                EncCellNames = new List<string> { "" },
                Frequency = $"0 * {todayDay} * *",
                Type = "",
                KeyFileType = "",
                AllowedUsers = new List<string>(),
                AllowedUserGroups = new List<string>(),
                Tags = new List<Tag>(),
                ReadMeSearchFilter = "",
                BatchExpiryInDays = 1,
                IsEnabled = "No"
            },
        };
            return configurations;
        }

        private Dictionary<string, string> GetDuplicateConfigJson()
        {
            dictionary.Add("DuplicateConfigName.json", DuplicateConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetConfigJsonWithIncorrectExchangeSetStandard()
        {
            dictionary.Add("BES.json", ConfigJsonWithIncorrectExchangeSetStandard);
            return dictionary;
        }
    }
}
