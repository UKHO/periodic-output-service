using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    [ExcludeFromCodeCoverage]
    public class PKSResponse
    {
        [XmlElement(ElementName = "cellname")]
        public string productName { get; set; }
        public int edition { get; set; }
        [XmlElement(ElementName = "permit")]
        public string key { get; set; }
    }
}
