namespace UKHO.PeriodicOutputService.Common.Helpers;
public interface IAzureMessageQueueHelper
{
    Task AddMessageAsync(string message);
}
