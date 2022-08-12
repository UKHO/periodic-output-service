namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        void CreateFileCopy(string filePath, Stream stream);
        IEnumerable<string> GetAllFiles(string directoryPath, SearchOption searchOption);
        void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory);
        void CreateIsoAndSha1(string targetPath, string directoryPath);
    }
}
