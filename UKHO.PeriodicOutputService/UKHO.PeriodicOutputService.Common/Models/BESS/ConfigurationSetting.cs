namespace UKHO.PeriodicOutputService.Common.Models.BESS
{
    public class ConfigurationSetting
    {
        public string Name { get; set; }
        //public bool IsEncrypted { get; set; }
        public string ExchangeSetStandard { get; set; }
        public IEnumerable<string> EncCellNames { get; set; }
        public string Frequency { get; set; }
        public string Type { get; set; }
        public string KeyFileType { get; set; }
        public IEnumerable<string> AllowedUsers { get; set; }
        public IEnumerable<string> AllowedUserGroups { get; set; }
        //public IEnumerable<KeyValuePair<string, string>> Tags { get; set; }
        public IEnumerable<Tag> Tags { get; set; }
        public string ReadMeSearchFilter { get; set; }
        public int BatchExpiryInDays { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class Tag
    {
        public string key { get; set; }
        public string value { get; set; }
    }
}
