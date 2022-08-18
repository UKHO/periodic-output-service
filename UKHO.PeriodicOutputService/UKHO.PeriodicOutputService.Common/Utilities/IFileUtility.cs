namespace UKHO.PeriodicOutputService.Common.Utility
{
    public interface IFileUtility
    {
        void CreateISOImage(IEnumerable<string> srcFiles, string targetPath, string directoryPath);

        void CreateSha1File(string targetPath);
    }
}
