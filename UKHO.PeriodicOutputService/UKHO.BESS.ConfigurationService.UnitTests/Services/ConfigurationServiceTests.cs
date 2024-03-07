using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
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
        private IConfiguration fakeConfiguration;
        private const string InvalidConfigJson = "[{\"Name\":,\"ExchangeSetStandard\":null,\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";
        private const string ValidConfigJson = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";
        private const string InvalidEmptyJson = "[{,,,}]";
        private const string InvalidConfigJsonWithInvalidEncCellNames = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\";\"GB234567\":\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";

        private Dictionary<string, string> dictionary;

        [SetUp]
        public void SetUp()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakeLogger = A.Fake<ILogger<ConfigurationService.Services.ConfigurationService>>();
            fakeConfiguration = A.Fake<IConfiguration>();
            configurationService = new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfiguration);
            dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureBlobStorageClient = () => new ConfigurationService.Services.ConfigurationService(null, fakeAzureTableStorageHelper, fakeLogger, fakeConfiguration);

            nullAzureBlobStorageClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageClient");

            Action nullAzureTableHelper = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, null, fakeLogger, fakeConfiguration);

            nullAzureTableHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullConfigurationServiceLogger = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, null, fakeConfiguration);

            nullConfigurationServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullConfigurationServiceConfiguration = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, null);

            nullConfigurationServiceConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");
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
        public void WhenInvalidEncCellNameIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetInvalidEncCellNameJson());
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

        private Dictionary<string, string> GetInvalidEncCellNameJson()
        {
            dictionary.Add("InvalidEncCellName.json", InvalidConfigJsonWithInvalidEncCellNames);
            return dictionary;
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessful_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsThrowsException_ThenReturnsFalse()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Throws<Exception>();

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

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

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsNotAddedToQueue_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());


            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsNotAddedToQueueSameDay_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueueOnSameDay());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingNotEnabled(), GetFakeSalesCatalogueDataProductResponse());


            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndWhenNextScheduleDetailsIsNull_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1"))!.Returns(null);

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndConfigurationSettingsHasInvalidCell_ThenLogDetails()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingWithInvalidEncCell(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() == "Invalid pattern or cell found : {InvalidEncCellName} | EncCellNames : {EncCellName} | _X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenConfigurationSettingsHasInvalidCellAndInvalidPattern_ThenScheduleDetailsNotAddedToQueue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingWithInvalidEncCellAndInvalidPattern(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "All listed cells are not found and neither cell matching with the pattern, bespoke will not create for : {EncCellName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustNotHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScsResponseDataHasAioCell_ThenRemoveAioCell()
        {
            fakeConfiguration["AioCells"] = "GB800001";

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            var salesCatalogueDataProductResponse = GetFakeSalesCatalogueDataProductResponse();
            salesCatalogueDataProductResponse.Add(new()
            {
                BaseCellEditionNumber = 6,
                BaseCellIssueDate = DateTime.UtcNow.AddDays(-30),
                BaseCellLocation = "M2;B1",
                BaseCellUpdateNumber = null,
                CancelledCellReplacements = new(),
                CellLimitEasternmostLatitude = 121,
                CellLimitNorthernmostLatitude = 25,
                CellLimitSouthernmostLatitude = 24,
                CellLimitWesternmostLatitude = 120,
                Compression = true,
                Encryption = false,
                FileSize = 265446,
                IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                IssueDatePreviousUpdate = null,
                LastUpdateNumberPreviousEdition = null,
                LatestUpdateNumber = 6,
                ProductName = fakeConfiguration["AioCells"]!
            });

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), salesCatalogueDataProductResponse);

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bess Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

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
                EncCellNames = new List<string> { "1U320240", "US*", "US123456", "US78910", "GB*"},
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

        private List<SalesCatalogueDataProductResponse> GetFakeSalesCatalogueDataProductResponse()
        {
            List<SalesCatalogueDataProductResponse> salesCatalogueDataProductResponses = new()
            {
                new()
                {
                    BaseCellEditionNumber = 3,
                    BaseCellIssueDate = DateTime.UtcNow.AddDays(-30),
                    BaseCellLocation = "M1;B2",
                    BaseCellUpdateNumber = null,
                    CancelledCellReplacements = new(),
                    CellLimitEasternmostLatitude = 121,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 120,
                    Compression = true,
                    Encryption = false,
                    FileSize = 265446,
                    IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                    IssueDatePreviousUpdate = null,
                    LastUpdateNumberPreviousEdition = null,
                    LatestUpdateNumber = 6,
                    ProductName = "1U320240"
                },new()
                {
                    BaseCellEditionNumber = 5,
                    BaseCellIssueDate = DateTime.UtcNow.AddDays(-30),
                    BaseCellLocation = "M1;B3",
                    BaseCellUpdateNumber = null,
                    CancelledCellReplacements = new(),
                    CellLimitEasternmostLatitude = 121,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 120,
                    Compression = true,
                    Encryption = false,
                    FileSize = 265446,
                    IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                    IssueDatePreviousUpdate = null,
                    LastUpdateNumberPreviousEdition = null,
                    LatestUpdateNumber = 6,
                    ProductName = "US5NY3DD"
                },new()
                {
                    BaseCellEditionNumber = 4,
                    BaseCellIssueDate = DateTime.UtcNow.AddDays(-30),
                    BaseCellLocation = "M3;B2",
                    BaseCellUpdateNumber = null,
                    CancelledCellReplacements = new(),
                    CellLimitEasternmostLatitude = 121,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 120,
                    Compression = true,
                    Encryption = false,
                    FileSize = 265446,
                    IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                    IssueDatePreviousUpdate = null,
                    LastUpdateNumberPreviousEdition = null,
                    LatestUpdateNumber = 6,
                    ProductName = "US4AK3KR"
                },new()
                {
                    BaseCellEditionNumber = 4,
                    BaseCellIssueDate = DateTime.UtcNow.AddDays(-30),
                    BaseCellLocation = "M3;B2",
                    BaseCellUpdateNumber = null,
                    CancelledCellReplacements = new(),
                    CellLimitEasternmostLatitude = 121,
                    CellLimitNorthernmostLatitude = 25,
                    CellLimitSouthernmostLatitude = 24,
                    CellLimitWesternmostLatitude = 120,
                    Compression = true,
                    Encryption = false,
                    FileSize = 265446,
                    IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                    IssueDatePreviousUpdate = null,
                    LastUpdateNumberPreviousEdition = null,
                    LatestUpdateNumber = 6,
                    ProductName = "GB800005"
                }
            };
            return salesCatalogueDataProductResponses;
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
                EncCellNames = new List<string> { "1U320240", "US*" },
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

        private List<BessConfig> GetFakeConfigurationSettingWithInvalidEncCell()
        {
            int todayDay = DateTime.UtcNow.Day;
            List<BessConfig> configurations = new()
            {
                new()
                {
                    Name = "BESS-1",
                    ExchangeSetStandard = "s63",
                    EncCellNames = new List<string> { "1U320240", "US*", "Test" },
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

        private List<BessConfig> GetFakeConfigurationSettingWithInvalidEncCellAndInvalidPattern()
        {
            int todayDay = DateTime.UtcNow.Day;
            List<BessConfig> configurations = new()
            {
                new()
                {
                    Name = "BESS-1",
                    ExchangeSetStandard = "s63",
                    EncCellNames = new List<string> { "1U320", "U*S" },
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
    }
}
