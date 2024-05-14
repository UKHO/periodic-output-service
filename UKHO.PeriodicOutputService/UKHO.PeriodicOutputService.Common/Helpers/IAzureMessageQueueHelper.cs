namespace UKHO.PeriodicOutputService.Common.Helpers;

public interface IAzureMessageQueueHelper
{
    Task AddMessageAsync(string message, string configName, string fileName, string builderService_CorrelationId);
}
