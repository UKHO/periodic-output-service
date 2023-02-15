namespace UKHO.PeriodicOutputService.Common.Models.Fss
{
    public class FssBatchFile
    {
        public string FileName { get; set; }
        public string FileLink { get; set; }
        public long FileSize { get; set; }
        public string VolumeIdentifier { get; set; }
    }
}
