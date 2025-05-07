using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Models.Ess;
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

        void CreateIsoAndSha1(string targetPath, string directoryPath, string volumeIdentifier);

        void CreateXmlFile(byte[] fileContent, string targetPath);

        void CreateZipFile(string sourceDirectoryName, string destinationArchiveFileName, bool deleteOldArchive = false);

        string ReadFileText(string filePath);

        bool CreateFileContent(string filePath, string content);

        void DeleteFile(string filePath);

        void DeleteFolder(string folderPath);

        IEnumerable<ProductVersion> GetProductVersionsFromDirectory(string sourcePath, string cellName);

        IEnumerable<ProductVersion> GetProductVersionsFromDirectory(string sourcePath);

        bool CreateEmptyFileContent(string filePath);

        bool DownloadReadmeFile(string filePath, Stream fileStream);

        Task CreateXmlFromObject<T>(T obj, string filePath, string fileName);

        void CreateTextFile(string filePath, string fileName, string content);
    }
}
