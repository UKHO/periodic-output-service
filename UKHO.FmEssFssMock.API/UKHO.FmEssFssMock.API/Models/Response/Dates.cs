using Newtonsoft.Json;

namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class Dates
    {
        public int UpdateNumber { get; set; }
        public DateTime? UpdateApplicationDate { get; set; }

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public DateTime IssueDate { get; set; }
    }
}
