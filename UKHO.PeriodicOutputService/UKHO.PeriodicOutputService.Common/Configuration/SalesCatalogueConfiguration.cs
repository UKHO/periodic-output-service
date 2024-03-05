using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.PeriodicOutputService.Common.Configuration
{
    [ExcludeFromCodeCoverage]
    public class SalesCatalogueConfiguration
    {
        public string BaseUrl { get; set; }
        public string Version { get; set; }
        public string ProductType { get; set; }
        public string ResourceId { get; set; }
        public string CatalogueType { get; set; }
    }
}
