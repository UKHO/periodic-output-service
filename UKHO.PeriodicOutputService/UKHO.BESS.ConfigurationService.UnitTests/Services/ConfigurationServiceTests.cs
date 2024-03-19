using System.Net;
using FakeItEasy;
using FluentAssertions;
using FluentValidation.Results;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.BESS.ConfigurationService.Validation;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Logging;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.Scs.Response;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;
using UKHO.PeriodicOutputService.Common.Services;

namespace UKHO.BESS.ConfigurationService.UnitTests.Services
{
    [TestFixture]
    public class ConfigurationServiceTests
    {
        private IConfigurationService configurationService;
        private IAzureBlobStorageClient fakeAzureBlobStorageClient;
        private IAzureTableStorageHelper fakeAzureTableStorageHelper;
        private IAzureBlobStorageService fakeAzureBlobStorageService;
        private ILogger<ConfigurationService.Services.ConfigurationService> fakeLogger;
        private ISalesCatalogueService fakeSalesCatalogueService;
        private const string InvalidConfigJson = "[{\"Name\":,\"ExchangeSetStandard\":null,\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"no\"}]";

        private const string ValidConfigJson = "[{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}," +
                                               "{\"Name\":\"Abc\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}]";

        private const string InvalidEmptyJson = "[{,,,}]";

        private const string DuplicateConfigJson = "[{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"yes\"}," +
                                                   "{\"Name\":\"Xyz\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";

        private const string ConfigJsonWithIncorrectExchangeSetStandard = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";
        private const string InvalidConfigJsonWithInvalidEncCellNames = "[{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\";\"GB234567\":\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}]";
        private Dictionary<string, string> dictionary;
        private IConfigValidator fakeConfigValidator;
        private IConfiguration fakeConfiguration;


        [SetUp]
        public void SetUp()
        {
            fakeAzureBlobStorageClient = A.Fake<IAzureBlobStorageClient>();
            fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
            fakeLogger = A.Fake<ILogger<ConfigurationService.Services.ConfigurationService>>();
            fakeConfigValidator = A.Fake<IConfigValidator>();
            fakeSalesCatalogueService = A.Fake<ISalesCatalogueService>();
            fakeConfiguration = A.Fake<IConfiguration>();
            fakeAzureBlobStorageService = A.Fake<IAzureBlobStorageService>();

            configurationService = new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService);
            dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureBlobStorageClient = () => new ConfigurationService.Services.ConfigurationService(null, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService);

            nullAzureBlobStorageClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageClient");

            Action nullAzureTableHelper = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, null, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService);

            nullAzureTableHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullConfigurationServiceLogger = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, null, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService);

            nullConfigurationServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullConfigValidator = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, null, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService);

            nullConfigValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configValidator");

            Action nullSalesCatalogueService = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, null, fakeConfiguration, fakeAzureBlobStorageService);
            nullSalesCatalogueService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueService");

            Action nullConfigurationServiceConfiguration = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, null, fakeAzureBlobStorageService);

            nullConfigurationServiceConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");

            Action nullAzureBlobStorageService = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, null);

            nullAzureBlobStorageService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageService");
        }

        [Test]
        public void WhenValidConfigIsFound_ThenConfigIsAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetValidConfigFilesJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
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
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
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
        public void WhenInvalidEncCellNameIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetInvalidEncCellNameJson());
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

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
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
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
                  ).MustNotHaveHappened();

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
                  ).MustNotHaveHappened();
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

        [Test]
        public void WhenInvalidConfigValidating_ThenValidationThrowsException()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainer()).Returns(GetConfigJsonWithIncorrectExchangeSetStandard());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Throws<Exception>();

            configurationService.ProcessConfigs();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationError.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while validating Bess config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
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
                "Exception occurred while processing Bess config {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            Assert.That(result, Is.False);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsReturnsFalseAndScheduleDetailsAddedToQueue_ThenLogWithMessagAdded()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueue(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored)).Returns(true);
            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());


            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, true)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueue(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored))
                .MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessMessageAddedInTheQueue.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Message is added in the queue for file:{FileName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsReturnsFalseAndScheduleDetailsAddedToQueue_ThenLogWithMessageNotAdded()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueue(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored)).Returns(false);

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());


            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, true)).MustHaveHappenedOnceOrMore();            

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessMessageNotAddedInTheQueue.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Message is not added in the queue, Bespoke Exchange Set will not be created for file:{FileName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsAddedToQueueAndFileSizeIsGreater_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("0");

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "ES size {fileSizeInMb}MB which is more than threshold :{BESSize}MB, Bespoke Exchange Set will not be created for file:{FileName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScheduleDetailsNotAddedToQueue_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());


            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, false)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueue(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored))
                .MustNotHaveHappened();

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
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingWithInvalidEncCell(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bess Config file: {FileName}, Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() == "Invalid pattern or ENC cell names found : {InvalidEncCellName} | AIO cells excluded : {AIOCellName} | _X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenConfigurationSettingsHasInvalidCellAndInvalidPattern_ThenScheduleDetailsNotAddedToQueue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            bool result = configurationService.CheckConfigFrequencyAndSaveQueueDetails(GetFakeConfigurationSettingWithInvalidEncCellAndInvalidPattern(), GetEmptySalesCatalogueDataProductResponses());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetail(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Neither listed ENC cell names found nor the pattern matched for any cell, Bespoke Exchange Set will not be created for file:{FileName} with ENC cells {EncCellNames} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            Assert.That(result, Is.True);
        }

        [Test]
        public void WhenCheckConfigFrequencyAndSaveQueueDetailsIsSuccessfulAndScsResponseDataHasAioCell_ThenRemoveAioCell()
        {
            fakeConfiguration["AioCells"] = "GB800001";

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetail("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");


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
                "Bess Config file: {FileName}, Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
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

        private List<SalesCatalogueDataProductResponse> GetEmptySalesCatalogueDataProductResponses()
        {
            return new List<SalesCatalogueDataProductResponse>();
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

        #region GetSalesCatalogueDataProductResponse
        private SalesCatalogueDataResponse GetSalesCatalogueDataResponse()
        {
            return new SalesCatalogueDataResponse
            {
                ResponseCode = HttpStatusCode.OK,
                ResponseBody = new List<SalesCatalogueDataProductResponse>()
                {
                    new ()
                    {
                        ProductName="10000002",
                        LatestUpdateNumber=5,
                        FileSize=600,
                        CellLimitSouthernmostLatitude=24,
                        CellLimitWesternmostLatitude=119,
                        CellLimitNorthernmostLatitude=25,
                        CellLimitEasternmostLatitude=120
                    }
                }
            };
        }
        #endregion
    }
}
