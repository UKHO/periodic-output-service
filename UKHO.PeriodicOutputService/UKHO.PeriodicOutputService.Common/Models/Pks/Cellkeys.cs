using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace UKHO.PeriodicOutputService.Common.Models.Pks
{
    [ExcludeFromCodeCoverage]
    [XmlRoot(ElementName = "cellkeys")]
    public class Cellkeys
    {
        [XmlElement(ElementName = "cell")]
        public List<ProductKeyServiceResponse> ProductKeyServiceResponses { get; set; }
    }
}
