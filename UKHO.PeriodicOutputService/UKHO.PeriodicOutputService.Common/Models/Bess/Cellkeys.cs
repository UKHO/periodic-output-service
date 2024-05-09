using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;
using UKHO.PeriodicOutputService.Common.Models.Pks;

namespace UKHO.PeriodicOutputService.Common.Models.Bess
{
    [ExcludeFromCodeCoverage]
    [XmlRoot(ElementName = "cellkeys")]
    public class Cellkeys
    {
        [XmlElement(ElementName = "cell")]
        public List<ProductKeyServiceResponse> ProductKeyServiceResponses { get; set; }
    }
}
