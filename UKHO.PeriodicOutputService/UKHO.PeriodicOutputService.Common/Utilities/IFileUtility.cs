namespace UKHO.PeriodicOutputService.Common.Utilities
{
    public interface IFileUtility
    {
        void CreateISOImage(IEnumerable<string> srcFiles, string targetPath, string directoryPath);

        void CreateSha1File(string targetPath);
    }
}
