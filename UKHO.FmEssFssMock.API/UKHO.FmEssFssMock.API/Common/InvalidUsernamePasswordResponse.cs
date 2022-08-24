namespace UKHO.FmEssFssMock.API.Common
{
    public class InvalidUsernamePasswordResponse
    {
        public string? CorrelationId { get; set; }
        public IEnumerable<InvalidUsernamePasswordErrors>? Errors { get; set; }
    }
    public class InvalidUsernamePasswordErrors
    {
        public string? Source { get; set; }
        public string? Description { get; set; }
    }
}
