using Newtonsoft.Json;

namespace UKHO.EssFssMock.API.Models.Bess
{
    public class Tag
    {
        [JsonProperty("key")]
        public string Key { get; set; }

        [JsonProperty("value")]
        public string Value { get; set; }
    }
}
