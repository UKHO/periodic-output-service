using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        byte[] ConvertStreamToByteArray(Stream input);
        void CreateFileCopy(string filePath, Stream stream);
        void CreateZipFile(string sourcePath, string destinationFilePath, bool deleteSourceDirectory);
        void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory);
        FileInfo GetFileInfo(string filePath);
        IEnumerable<string> GetFiles(string directoryPath, string fileExtension, SearchOption searchOption);
        byte[] GetFileInBytes(UploadFileBlockRequestModel UploadBlockMetaData);
        void CreateIsoAndSha1(string targetPath, string directoryPath);
    }
}
