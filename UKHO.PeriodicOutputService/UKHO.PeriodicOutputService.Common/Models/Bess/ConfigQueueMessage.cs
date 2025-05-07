namespace UKHO.PeriodicOutputService.Common.Models.Bess;

public class ConfigQueueMessage
{
    public string Name { get; set; }
    public string ExchangeSetStandard { get; set; }
    public string Frequency { get; set; }
    public string Type { get; set; }
    public string KeyFileType { get; set; }
    public IEnumerable<string> AllowedUsers { get; set; }
    public IEnumerable<string> AllowedUserGroups { get; set; }
    public IEnumerable<Tag> Tags { get; set; }
    public string ReadMeSearchFilter { get; set; }
    public int BatchExpiryInDays { get; set; }
    public string IsEnabled { get; set; }
    public string FileName { get; set; }
    public long? FileSize { get; set; }
    public string CorrelationId { get; set; }
    public string MessageDetailUri { get; set; }
}

public class MessageDetail
{
    public IEnumerable<string> EncCellNames { get; set; }
}
