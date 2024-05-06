namespace UKHO.FmEssFssMock.API.Models.Response
{
    public class ConfigQueueMessage
    {
        public IEnumerable<string> AllowedUsers { get; set; }
        public IEnumerable<string> AllowedUserGroups { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public int BatchExpiryInDays { get; set; }
    }

    public class Tag
    {
        public string Key { get; set; }
        public string Value { get; set; }
    }
}
