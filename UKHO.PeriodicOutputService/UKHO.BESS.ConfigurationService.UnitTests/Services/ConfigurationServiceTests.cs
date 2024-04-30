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
        private IMacroTransformer fakeMacroTransformer;

        private const string UndefinedValuesConfigJson = "{\"name\":,\"exchangeSetStandard\":null,\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"no\"}";

        private const string ValidConfigWithoutMacroJson = "{\"NAME\":\"Xyz\",\"exchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"Year\",\"value\":\"2025\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string ValidConfigWithEmptyOrNullKeyInTagsJson = "{\"NAME\":\"Xyz\",\"exchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\" \",\"value\":\"2025\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string ValidConfigWithNullMacroValueJson = "{\"NAME\":\"Xyz\",\"exchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"Year\",\"value\":null},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string ValidConfigWithValidMacroJson = "{\"name\":\"Xyz\",\"exchangeSetStandard\":\"s63\",\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"Year\",\"value\":\"$(now.Year)\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string ValidConfigWithInvalidMacroJson = "{\"name\":\"Xyz\",\"exchangeSetStandard\":\"s63\",\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"Year\",\"value\":\"$(now.Yea)\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string AnotherValidConfigJson = "{\"NAME\":\"Abc\",\"exchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string EmptyConfigJson = "{,,,}";

        private const string DuplicateConfigJson = "{\"name\":\"Xyz\",\"exchangeSetStandard\":\"s57\",\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}";

        private const string ConfigWithJsonError = "[{\"name\":\"Xyz\",\"exchangeSetStandard\":\"s57\",\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"yes\"}]";

        private const string ConfigJsonWithIncorrectExchangeSetStandard = "{\"name\":\"Xyz.json\",\"exchangeSetStandard\":\"s\",\"encCellNames\":[\"GB123456\",\"GB234567\",\"GB*\",\"GB1*\"],\"frequency\":\"15 16 2 2 *\",\"type\":\"BASE\",\"keyFileType\":\"NONE\",\"allowedUsers\":[\"User1\",\"User2\"],\"allowedUserGroups\":[\"UG1\",\"UG2\"],\"tags\":[{\"key\":\"key1\",\"value\":\"value1\"},{\"key\":\"key2\",\"value\":\"value2\"}],\"readMeSearchFilter\":\"\",\"batchExpiryInDays\":30,\"isEnabled\":\"Yes\"}";

        private const string InvalidConfigJsonWithInvalidEncCellNames = "{\"Name\":\"Xyz.json\",\"ExchangeSetStandard\":\"s63\",\"EncCellNames\":[\"GB123456\";\"GB234567\":\"GB*\",\"GB1*\"],\"Frequency\":\"15 16 2 2 *\",\"Type\":\"BASE\",\"KeyFileType\":\"NONE\",\"AllowedUsers\":[\"User1\",\"User2\"],\"AllowedUserGroups\":[\"UG1\",\"UG2\"],\"Tags\":[{\"Key\":\"key1\",\"Value\":\"value1\"},{\"Key\":\"key2\",\"Value\":\"value2\"}],\"ReadMeSearchFilter\":\"\",\"BatchExpiryInDays\":30,\"IsEnabled\":\"Yes\"}";
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
            fakeMacroTransformer = A.Fake<IMacroTransformer>();

            configurationService = new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);
            dictionary = new Dictionary<string, string>();
        }

        [Test]
        public void WhenParameterIsNull_ThenConstructorThrowsArgumentNullException()
        {
            Action nullAzureBlobStorageClient = () => new ConfigurationService.Services.ConfigurationService(null, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);

            nullAzureBlobStorageClient.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageClient");

            Action nullAzureTableHelper = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, null, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);

            nullAzureTableHelper.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureTableStorageHelper");

            Action nullConfigurationServiceLogger = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, null, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);

            nullConfigurationServiceLogger.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("logger");

            Action nullConfigValidator = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, null, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);

            nullConfigValidator.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configValidator");

            Action nullSalesCatalogueService = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, null, fakeConfiguration, fakeAzureBlobStorageService, fakeMacroTransformer);
            nullSalesCatalogueService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("salesCatalogueService");

            Action nullConfigurationServiceConfiguration = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, null, fakeAzureBlobStorageService, fakeMacroTransformer);

            nullConfigurationServiceConfiguration.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("configuration");

            Action nullAzureBlobStorageService = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, null, fakeMacroTransformer);

            nullAzureBlobStorageService.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("azureBlobStorageService");

            Action nullMacroTransformer = () => new ConfigurationService.Services.ConfigurationService(fakeAzureBlobStorageClient, fakeAzureTableStorageHelper, fakeLogger, fakeConfigValidator, fakeSalesCatalogueService, fakeConfiguration, fakeAzureBlobStorageService, null);
            nullMacroTransformer.Should().ThrowExactly<ArgumentNullException>().And.ParamName.Should().Be("macroTransformer");
        }

        [Test]
        public void WhenAValidConfigIsFound_ThenConfigIsAddedToListAndProcessedFurther()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigFilesJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public void WhenAValidConfigWithValidMacroIsFound_ThenConfigIsFurtherProcessed()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigsWithMacroExpressionJson(ValidConfigWithValidMacroJson));
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
            A.CallTo(() => fakeMacroTransformer.ExpandMacros(A<string>.Ignored)).Returns("2024");

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public void WhenAValidConfigWithInvalidOrUnavailableMacroIsFound_ThenConfigIsNotFurtherProcessed()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigsWithMacroExpressionJson(ValidConfigWithInvalidMacroJson));
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
            A.CallTo(() => fakeMacroTransformer.ExpandMacros(A<string>.Ignored)).Returns("$(now.Yea)");

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.MacroInvalidOrUnavailable.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Macro {macro} is invalid or not available, Bespoke Exchange Set will not be created for config file: {fileName} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenValueOfKeyInTagsIsNullOrEmpty_ThenThrowsValidationError()
        {
            var validationMessage = new ValidationFailure("Tags", "Key or Value not provided");
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigsWithMacroExpressionJson(ValidConfigWithNullMacroValueJson));
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigInvalidAttributes.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenKeyInTagsIsNullOrEmpty_ThenThrowValidationError()
        {
            var validationMessage = new ValidationFailure("Tags", "Key or Value not provided");
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigsWithMacroExpressionJson(ValidConfigWithEmptyOrNullKeyInTagsJson));
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigInvalidAttributes.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenAValidConfigWithValidMacroIsNotTransformed_ThenTransformMacrosThrowsException()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetValidConfigsWithMacroExpressionJson(ValidConfigWithValidMacroJson));
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());
            A.CallTo(() => fakeMacroTransformer.ExpandMacros(A<string>.Ignored)).Throws<Exception>();

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.MacroTransformationFailed.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Exception occurred while transforming macros {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenMoreThanOneValidConfigIsFound_ThenConfigsAreAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetMoreThanOneValidConfigFilesJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public void WhenEmptyConfigIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetEmptyConfigJson());
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing BESS config file : {fileName}. It might have missing or extra commas, missing brackets, or other syntax errors.| Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenConfigWithJsonErrorIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetConfigWithJsonError());
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing BESS config file : {fileName}. It might have missing or extra commas, missing brackets, or other syntax errors.| Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenInvalidEncCellNameIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetInvalidEncCellNameJson());
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigParsingError.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while parsing BESS config file : {fileName}. It might have missing or extra commas, missing brackets, or other syntax errors.| Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenUndefinedValuesFoundInConfig_ThenConfigIsNotAddedToList()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetUndefinedValuesConfigJson());
            A.CallTo(() => fakeSalesCatalogueService.GetSalesCatalogueData()).Returns(GetSalesCatalogueDataResponse());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                 call.Method.Name == "Log"
                 && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                 && call.GetArgument<EventId>(1) == EventIds.BessConfigValueNotDefined.ToEventId()
                 && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS config file : {fileName} contains undefined values. | _X-Correlation-ID : {CorrelationId}"
                 ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenGetConfigsInContainerAsyncMethodSendNull_ThenThrowsException()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Throws<Exception>();
            configurationService.Invoking(x => x.ProcessConfigsAsync()).Should().ThrowExactlyAsync<Exception>();

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
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(new Dictionary<string, string>());

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs not found");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
                  ).MustNotHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsNotFound.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs not found | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Information
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
                  ).MustNotHaveHappened();
        }

        [Test]
        public void WhenDuplicateConfigIsFound_ThenThrowsError()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetDuplicateConfigJson());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure>()));

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingStarted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing started, Total configs file count : {count}  | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsDuplicateRecordsFound.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS config file : {fileName} found with duplicate Name attribute : {name} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationSummary.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() ==
                "Configs validation summary, total configs : {totalConfigCount} | valid configs : {validFileCount} | configs with missing attributes or values : {invalidFileCount} | configs with json error : {filesWithJsonErrorCount} | configs with duplicate name attribute : {configsWithDuplicateNameAttributeCount} | configs with invalid macros {configsWithInvalidMacros} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigsProcessingCompleted.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS configs processing completed | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenIncorrectExchangeSetStandardIsFound_ThenThrowsValidationError()
        {
            var validationMessage = new ValidationFailure("ExchangeSetStandard", "Attribute value is invalid. Expected value is either s63 or s57");
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetConfigJsonWithIncorrectExchangeSetStandard());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Returns(new ValidationResult(new List<ValidationFailure> { validationMessage }));

            var result = configurationService.ProcessConfigsAsync();

            result.Result.Should().Be("BESS configs processing completed");

            A.CallTo(fakeLogger).Where(call =>
                  call.Method.Name == "Log"
                  && call.GetArgument<LogLevel>(0) == LogLevel.Error
                  && call.GetArgument<EventId>(1) == EventIds.BessConfigInvalidAttributes.ToEventId()
                  && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "BESS config file : {fileName} found with Validation errors. {errors} | _X-Correlation-ID : {CorrelationId}"
                  ).MustHaveHappened();
        }

        [Test]
        public async Task WhenInvalidConfigValidating_ThenValidationThrowsException()
        {
            A.CallTo(() => fakeAzureBlobStorageClient.GetConfigsInContainerAsync()).Returns(GetConfigJsonWithIncorrectExchangeSetStandard());
            A.CallTo(() => fakeConfigValidator.Validate(A<BessConfig>.Ignored)).Throws<Exception>();

            var result = await configurationService.ProcessConfigsAsync();

            result.Should().Be("Error occurred while validating BESS config file");

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.BessConfigValidationError.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Error occurred while validating BESS config file : {fileName} | Exception Message : {Message} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();
        }

        private Dictionary<string, string> GetValidConfigFilesJson()
        {
            dictionary.Add("Valid.json", ValidConfigWithoutMacroJson);
            return dictionary;
        }

        private Dictionary<string, string> GetValidConfigsWithMacroExpressionJson(string configWithMacro)
        {
            dictionary.Add($"{configWithMacro}.json", configWithMacro);
            return dictionary;
        }

        private Dictionary<string, string> GetMoreThanOneValidConfigFilesJson()
        {
            dictionary.Add("Valid.json", ValidConfigWithoutMacroJson);
            dictionary.Add("Valid2.json", AnotherValidConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetConfigWithJsonError()
        {
            dictionary.Add("ConfigWithJsonError.json", ConfigWithJsonError);
            return dictionary;
        }

        private Dictionary<string, string> GetUndefinedValuesConfigJson()
        {
            dictionary.Add("UndefinedValuesConfig.json", UndefinedValuesConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetEmptyConfigJson()
        {
            dictionary.Add("Empty.json", EmptyConfigJson);
            return dictionary;
        }

        private Dictionary<string, string> GetInvalidEncCellNameJson()
        {
            dictionary.Add("InvalidEncCellName.json", InvalidConfigJsonWithInvalidEncCellNames);
            return dictionary;
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessful_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();
            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncThrowsException_ThenReturnsFalse()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Throws<Exception>();

            Func<Task> act = async () => { await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse()); };
            await act.Should().ThrowAsync<FulfilmentException>().Where(x => x.EventId == EventIds.BessConfigFrequencyProcessingException.ToEventId());

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Error
                && call.GetArgument<EventId>(1) == EventIds.BessConfigFrequencyProcessingException.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Exception occurred while processing BESS config {DateTime} | {ErrorMessage} | StackTrace : {StackTrace} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappenedOnceExactly();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncReturnsTrueAndScheduleDetailsAddedToQueue_ThenLogWithMessagAdded()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored)).Returns(true);
            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, true)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored))
                .MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessQueueMessageSuccessful.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Queue message creation successful for file:{FileName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncReturnsFalseAndScheduleDetailsAddedToQueue_ThenLogWithMessageNotAdded()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored)).Returns(false);

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync(A<string>.Ignored))
                .MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, true)).MustHaveHappenedOnceOrMore();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessQueueMessageFailed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2).ToDictionary(c => c.Key, c => c.Value)["{OriginalFormat}"].ToString() == "Something went wrong while adding message to queue, Bespoke Exchange Set will not be created for file : {FileName} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndFileSizeIsGreater_ThenMessageNotAddedToQueue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            var fakeScsList = GetEmptySalesCatalogueDataProductResponses();
            fakeScsList.Add(new()
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
                FileSize = 734108057,
                IssueDateLatestUpdate = DateTime.UtcNow.AddDays(-60),
                IssueDatePreviousUpdate = null,
                LastUpdateNumberPreviousEdition = null,
                LatestUpdateNumber = 6,
                ProductName = "1U320240"
            });

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), fakeScsList);

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessSizeExceedsThreshold.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Bespoke Exchange Set size {fileSizeInMb}MB which is more than the threshold :{BESSize}MB, Bespoke Exchange Set will not be created for file : {FileName} | " + "_X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();
            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(A<BessConfig>.Ignored, A<IEnumerable<string>>.Ignored, A<int>.Ignored)).MustNotHaveHappened();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessfulAndScheduleDetailsNotAddedToQueue_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueue());

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, false)).MustHaveHappenedOnceOrMore();

            A.CallTo(() => fakeAzureBlobStorageService.SetConfigQueueMessageModelAndAddToQueueAsync(A<BessConfig>.Ignored, A<List<string>>.Ignored, A<int>.Ignored))
                .MustNotHaveHappened();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessfulAndScheduleDetailsNotAddedToQueueSameDay_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInQueueOnSameDay());

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSettingNotEnabled(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessfulAndWhenNextScheduleDetailsIsNull_ThenReturnsTrue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1"))!.Returns(Task.FromResult<ScheduleDetailEntity>(null));

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), GetFakeSalesCatalogueDataProductResponse());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessfulAndConfigurationSettingsHasInvalidCell_ThenLogDetails()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
            A.CallTo(() => fakeConfiguration["BESSizeInMB"]).Returns("700");

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSettingWithInvalidEncCell(), GetFakeSalesCatalogueDataProductResponse());

            result.Should().BeTrue();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigFrequencyElapsed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "BESS Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessInvalidEncCellNamesOrPatternNotFoundInSalesCatalogue.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() == "Invalid pattern or ENC cell names found : {InvalidEncCellName} | AIO cells to be excluded : {AIOCellName} | _X-Correlation-ID : {CorrelationId}").MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();
        }

        [Test]
        public async Task WhenConfigurationSettingsHasInvalidCellAndInvalidPattern_ThenScheduleDetailsNotAddedToQueue()
        {
            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSettingWithInvalidEncCellAndInvalidPattern(), GetEmptySalesCatalogueDataProductResponses());

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Warning
                && call.GetArgument<EventId>(1) == EventIds.BessEncCellNamesAndPatternNotFoundInSalesCatalogue.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "Neither listed ENC cell names found nor the pattern matched for any cell, Bespoke Exchange Set will not be created for : {EncCellNames} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            result.Should().BeTrue();
        }

        [Test]
        public async Task WhenCheckConfigFrequencyAndSaveQueueDetailsAsyncIsSuccessfulAndScsResponseDataHasAioCell_ThenRemoveAioCell()
        {
            fakeConfiguration["AioCells"] = "GB800001";

            A.CallTo(() => fakeAzureTableStorageHelper.GetScheduleDetailAsync("BESS-1")).Returns(GetFakeScheduleDetailsToAddInQueue());
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

            bool result = await configurationService.CheckConfigFrequencyAndSaveQueueDetailsAsync(GetFakeConfigurationSetting(), salesCatalogueDataProductResponse);

            result.Should().BeTrue();

            A.CallTo(fakeLogger).Where(call =>
                call.Method.Name == "Log"
                && call.GetArgument<LogLevel>(0) == LogLevel.Information
                && call.GetArgument<EventId>(1) == EventIds.BessConfigFrequencyElapsed.ToEventId()
                && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                    "{OriginalFormat}"].ToString() ==
                "BESS Config Name: {Name} with CRON ({Frequency}), Schedule At : {ScheduleTime}, Executed At : {Timestamp} | _X-Correlation-ID : {CorrelationId}"
            ).MustHaveHappened();

            A.CallTo(() =>
                fakeAzureTableStorageHelper.UpsertScheduleDetailAsync(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();
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
                    EncCellNames = new List<string> { "1U320240", "US*", "US123456", "US78910", "GB*" },
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
                },
                new()
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
                },
                new()
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
                },
                new()
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
            dictionary.Add("ValidConfig.json", ValidConfigWithoutMacroJson);
            dictionary.Add("DuplicateConfig.json", DuplicateConfigJson);
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
    }
}
