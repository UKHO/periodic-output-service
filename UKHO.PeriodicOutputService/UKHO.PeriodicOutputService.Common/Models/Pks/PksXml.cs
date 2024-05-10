using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using UKHO.PeriodicOutputService.Common.Models.Bess;

namespace UKHO.PeriodicOutputService.Common.Models.Pks
{
    [ExcludeFromCodeCoverage]
    [XmlRoot(ElementName = "CellPermitExchange")]
    public class PksXml
    {
        [XmlElement(ElementName = "date")]
        public string Date { get; set; }

        [XmlElement(ElementName = "cellkeys")]
        public Cellkeys Cellkeys { get; set; }

        [XmlAttribute(AttributeName = "noNamespaceSchemaLocation", Namespace = "http://www.w3.org/2001/XMLSchema-instance")]
        public string NoNamespaceSchemaLocation = ".\\UKHO.PeriodicOutputService\\UKHO.PeriodicOutputService.Common\\XmlSchema\\CellPermitExchange.xsd";
    }
}
