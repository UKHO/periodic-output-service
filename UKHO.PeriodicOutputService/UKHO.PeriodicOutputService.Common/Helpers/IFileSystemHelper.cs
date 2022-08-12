namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        void CreateFileCopy(string filePath, Stream stream);
    }
}
