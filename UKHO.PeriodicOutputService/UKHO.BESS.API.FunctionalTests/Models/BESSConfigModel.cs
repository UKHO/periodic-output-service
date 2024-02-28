namespace UKHO.BESS.API.FunctionalTests.Models
{
    public class BESSConfigModel
    {
        public string? Name { get; set; }
        public bool IsEncrypted { get; set; }
        public List<string>? EncCellNames { get; set; }
        public string? Frequency { get; set; }
        public string? Type { get; set; }
        public string? KeyFileType { get; set; }
        public List<string>? AllowedUsers { get; set; }
        public List<string>? AllowedUserGroups { get; set; }
        public List<Tag>? Tags { get; set; }
        public string? ReadMeSearchFilter { get; set; }
        public int BatchExpiryInDays { get; set; }
        public bool IsEnabled { get; set; }
    }

    public class Tag
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    }
}
