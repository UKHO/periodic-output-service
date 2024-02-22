using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Models.Bess
{
    [JsonObject(ItemNullValueHandling = NullValueHandling.Ignore)]
    public class BessConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        [JsonProperty("exchangeSetStandard")]
        public string ExchangeSetStandard { get; set; }
        [JsonProperty("encCellNames")]
        public IEnumerable<string> EncCellNames { get; set; }
        [JsonProperty("frequency")]
        public string Frequency { get; set; }
        [JsonProperty("type")]
        public string Type { get; set; }
        [JsonProperty("keyFileType")]
        public string KeyFileType { get; set; }
        [JsonProperty("allowedUsers")]
        public IEnumerable<string> AllowedUsers { get; set; }
        [JsonProperty("allowedUserGroups")]
        public IEnumerable<string> AllowedUserGroups { get; set; }
        [JsonProperty("tags")]
        public IEnumerable<Tag> Tags { get; set; }
        [JsonProperty("readMeSearchFilter")]
        public string ReadMeSearchFilter { get; set; }
        [JsonProperty("batchExpiryInDays")]
        public int BatchExpiryInDays { get; set; }
        [JsonProperty("isEnabled")]
        public bool? IsEnabled { get; set; }
    }

    public class Tag
    {
        [JsonProperty("key")]
        public string Key { get; set; }
        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
