using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    public class BessConfig
    {
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Name { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ExchangeSetStandard { get; set; }
        public IEnumerable<string> EncCellNames { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Frequency { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string KeyFileType { get; set; }
        public IEnumerable<string> AllowedUsers { get; set; }
        public IEnumerable<string> AllowedUserGroups { get; set; }
        public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string ReadMeSearchFilter { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int BatchExpiryInDays { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public bool IsEnabled { get; set; }
    }
}
