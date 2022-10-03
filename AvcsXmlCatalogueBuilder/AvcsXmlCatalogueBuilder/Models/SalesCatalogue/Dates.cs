using Newtonsoft.Json;
using System;

namespace AvcsXmlCatalogueBuilder.Models.SalesCatalogue
{
    public class Dates
    {
        public int UpdateNumber { get; set; }
        public DateTime? UpdateApplicationDate { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IssueDate { get; set; }
    }
}
