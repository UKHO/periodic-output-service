namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IZipHelper
    {
        void CreateZipFile(string sourcePath, string destinationPath);
        void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName);
    }
}
