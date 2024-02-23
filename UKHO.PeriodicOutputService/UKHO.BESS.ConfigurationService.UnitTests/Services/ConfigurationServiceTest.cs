using FakeItEasy;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using UKHO.BESS.ConfigurationService.Services;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Bess;
using UKHO.PeriodicOutputService.Common.Models.TableEntities;

namespace UKHO.BESS.ConfigurationService.UnitTests.Services;

[TestFixture]
public class ConfigurationServiceTest
{
    private IConfigurationService _fakeConfigurationService;
    private IAzureTableStorageHelper _fakeAzureTableStorageHelper;
    private ILogger<ConfigurationService.Services.ConfigurationService> _fakeLogger;

    [SetUp]
    public void Setup()
    {
        _fakeConfigurationService = A.Fake<IConfigurationService>();
        _fakeAzureTableStorageHelper = A.Fake<IAzureTableStorageHelper>();
        _fakeLogger = A.Fake<ILogger<ConfigurationService.Services.ConfigurationService>>();
        _fakeConfigurationService =
            new ConfigurationService.Services.ConfigurationService(_fakeAzureTableStorageHelper, _fakeLogger);
    }

    [Test]
    public void Does_Constructor_Throws_ArgumentNullException_When_Parameter_Is_Null()
    {
        Assert.Throws<ArgumentNullException>(
                () => new ConfigurationService.Services.ConfigurationService(null, _fakeLogger))
            .ParamName
            .Should().Be("azureTableStorageHelper");
        Assert.Throws<ArgumentNullException>(
                () => new ConfigurationService.Services.ConfigurationService(_fakeAzureTableStorageHelper, null))
            .ParamName
            .Should().Be("logger");
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_True()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInMsgQueue());

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSetting());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();
        A.CallTo(() =>
            _fakeAzureTableStorageHelper.RefreshNextSchedule(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappened();

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details completed | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.True);
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_False_WhenExceptionOccurs()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1")).Throws<Exception>();

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSetting());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Error
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Exception at schedule config details {DateTime} | {ErrorMessage} | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.False);
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_True_WhenScheduleDetailsAddedToMsgQueue()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1")).Returns(GetFakeScheduleDetailsToAddInMsgQueue());

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSetting());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Running schedule config for Name : {Name} | Frequency : {Frequency}| _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappened();

        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails(A<string>.Ignored))
            .MustHaveHappened();

        A.CallTo(() =>
            _fakeAzureTableStorageHelper.RefreshNextSchedule(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();



        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details completed | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.True);
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_True_WhenScheduleDetailsNotAddedToMsgQueue()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInMsgQueue());

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSetting());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails(A<string>.Ignored))
            .MustHaveHappened();

        A.CallTo(() =>
            _fakeAzureTableStorageHelper.RefreshNextSchedule(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();


        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details completed | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.True);
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_True_WhenScheduleDetailsNotAddedToMsgQueueOnSameDay()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1")).Returns(GetFakeScheduleDetailsNotToAddInMsgQueueOnSameDay());

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSettingNotEnabled());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails(A<string>.Ignored))
            .MustHaveHappened();

        A.CallTo(() =>
            _fakeAzureTableStorageHelper.RefreshNextSchedule(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();


        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details completed | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.True);
    }

    [Test]
    public void Does_ScheduleConfigDetails_Returns_True_WhenNextScheduleDetailsIsNull()
    {
        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails("BESS-1"))!.Returns(null);

        bool result = _fakeConfigurationService.ScheduleConfigDetails(GetFakeConfigurationSetting());

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details started | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        A.CallTo(() => _fakeAzureTableStorageHelper.GetNextScheduleDetails(A<string>.Ignored))
            .MustHaveHappened();

        A.CallTo(() =>
            _fakeAzureTableStorageHelper.RefreshNextSchedule(A<DateTime>.Ignored, A<BessConfig>.Ignored, A<bool>.Ignored)).MustHaveHappenedOnceOrMore();

        A.CallTo(_fakeLogger).Where(call =>
            call.Method.Name == "Log"
            && call.GetArgument<LogLevel>(0) == LogLevel.Information
            && call.GetArgument<IEnumerable<KeyValuePair<string, object>>>(2)!.ToDictionary(c => c.Key, c => c.Value)[
                "{OriginalFormat}"].ToString() ==
            "Schedule config details completed | _X-Correlation-ID : {CorrelationId}"
        ).MustHaveHappenedOnceExactly();

        Assert.That(result, Is.True);
    }

    private ScheduleDetails GetFakeScheduleDetailsToAddInMsgQueue()
    {
        var history = new ScheduleDetails

        {
            PartitionKey = "BessConfigSchedule",
            RowKey = "BESS-1",
            NextScheduleTime = DateTime.UtcNow.AddMinutes(1),
            IsEnabled = true,
            IsExecuted = false,
        };
        return history;
    }

    private ScheduleDetails GetFakeScheduleDetailsNotToAddInMsgQueue()
    {
        var history = new ScheduleDetails

        {
            PartitionKey = "BessConfigSchedule",
            RowKey = "BESS-1",
            NextScheduleTime = DateTime.UtcNow.AddDays(1),
            IsEnabled = false,
            IsExecuted = true

        };
        return history;
    }

    private ScheduleDetails GetFakeScheduleDetailsNotToAddInMsgQueueOnSameDay()
    {
        var history = new ScheduleDetails

        {
            PartitionKey = "BessConfigSchedule",
            RowKey = "BESS-1",
            NextScheduleTime = DateTime.UtcNow,
            IsEnabled = true,
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
                IsEnabled = true
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
                IsEnabled = false
            },
        };
        return configurations;
    }
}
