using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BessConfig
    {
        public string Name { get; set; }
        public string ExchangeSetStandard { get; set; }
        public IEnumerable<string> EncCellNames { get; set; }
        public string Frequency { get; set; }
        public string Type { get; set; }
        public string KeyFileType { get; set; }
        public IEnumerable<string> AllowedUsers { get; set; }
        public IEnumerable<string> AllowedUserGroups { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public string ReadMeSearchFilter { get; set; }
        public int BatchExpiryInDays { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class Tag
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
