namespace UKHO.PeriodicOutputService.Common.Helpers;
public interface IAzureMessageQueueHelper
{
    Task AddMessage(string message);
}
