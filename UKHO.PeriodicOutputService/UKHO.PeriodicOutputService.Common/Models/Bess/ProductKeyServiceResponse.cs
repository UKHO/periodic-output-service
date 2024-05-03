using System.Xml.Serialization;
using Newtonsoft.Json;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    public class ProductKeyServiceResponse
    {
        [XmlElement(ElementName = "cellname")]
        [JsonProperty("productName")]
        public string ProductName { get; set; }

        [XmlElement(ElementName = "edition")]
        [JsonProperty("edition")]
        public string Edition { get; set; }

        [XmlElement(ElementName = "permit")]
        [JsonProperty("key")]
        public string Key { get; set; }
    }
}
