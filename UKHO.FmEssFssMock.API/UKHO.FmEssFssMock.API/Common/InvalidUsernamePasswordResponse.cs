namespace UKHO.FmEssFssMock.API.Common
{
    public class InvalidUsernamePasswordResponse
    {
        public string? correlationId { get; set; }
        public IEnumerable<InvalidUsernamePasswordErrors>? errors { get; set; }
    }
    public class InvalidUsernamePasswordErrors
    {
        public string? source { get; set; }
        public string? description { get; set; }
    }
}
