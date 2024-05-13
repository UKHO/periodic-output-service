using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Models.Pks
{
    public class ProductKeyServiceRequest
    {
        [JsonProperty("productName")]
        public string ProductName { get; set; }
        [JsonProperty("edition")]
        public string Edition { get; set; }
    }
}
