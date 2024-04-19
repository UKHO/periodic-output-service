using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    [ExcludeFromCodeCoverage]
    [XmlRoot(ElementName = "CellPermitExchange")]
    public class PKSXml
    {
        [XmlElement(ElementName = "date")]
        public string date { get; set; }

        [XmlElement(ElementName = "cellkeys")]
        public cellkeys cellkeys { get; set; }

        [XmlAttribute(AttributeName = "noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string noNamespaceSchemaLocation = ".\\UKHO.PeriodicOutputService\\UKHO.PeriodicOutputService.Common\\XmlSchema\\CellPermitExchange.xsd";
    }

    [ExcludeFromCodeCoverage]
    [XmlRoot(ElementName = "cellkeys")]
    public class cellkeys
    {
        [XmlElement(ElementName = "cell")]
        public List<PKSResponse> response { get; set; }
    }
}
