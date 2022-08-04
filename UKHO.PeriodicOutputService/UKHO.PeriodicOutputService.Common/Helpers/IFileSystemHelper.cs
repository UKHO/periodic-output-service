namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        byte[] ConvertStreamToByteArray(Stream input);
        void CreateFileCopy(string filePath, Stream stream);
    }
}
