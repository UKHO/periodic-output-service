using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        byte[] GetFileInBytes(UploadFileBlockRequestModel UploadBlockMetaData);
        void CreateFileCopy(string filePath, Stream stream);
        IFileInfo GetFileInfo(string filePath);
        IEnumerable<string> GetFiles(string directoryPath, string extensionsToSearch, SearchOption searchOption);
        List<FileDetail> GetFileMD5(IEnumerable<string> fileNames);
        IEnumerable<string> GetAllFiles(string directoryPath, SearchOption searchOption);
        void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory);
        void CreateIsoAndSha1(string targetPath, string directoryPath);
        void CleanupHomeDirectory(string path);
    }
}
