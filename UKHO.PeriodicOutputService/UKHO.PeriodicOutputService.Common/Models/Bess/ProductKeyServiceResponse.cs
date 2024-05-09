using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    public class ProductKeyServiceResponse
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [JsonProperty("edition")]
        public string Edition { get; set; }

        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
