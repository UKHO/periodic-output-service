using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public interface IFileSystemHelper
    {
        void CreateDirectory(string folderPath);
        IFileInfo GetFileInfo(string filePath);
        IEnumerable<string> GetFiles(string directoryPath, string extensionsToSearch, SearchOption searchOption);
        byte[] GetFileInBytes(UploadFileBlockRequestModel UploadBlockMetaData);
        List<FileDetail> GetFileMD5(IEnumerable<string> fileNames);
    }
}
