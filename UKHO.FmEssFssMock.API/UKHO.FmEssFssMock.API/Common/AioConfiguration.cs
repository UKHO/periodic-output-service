namespace UKHO.FmEssFssMock.API.Common
{
    public class AioConfiguration
    {
        public bool? AioEnabled { get; set; }
        public string AioCells { get; set; }
        public bool IsAioEnabled
        {
            get
            {
                return Convert.ToBoolean(AioEnabled.HasValue ? AioEnabled : false);
            }
            set
            {
                AioEnabled = value;
            }
        }
    }
}
