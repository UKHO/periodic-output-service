using System.IO.Abstractions;
using System.Security.Cryptography;
using DiscUtils.Iso9660;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;
      
        public FileSystemHelper(IFileSystem fileSystem)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
        }

        public void CreateDirectory(string folderPath)
        {
            if (!_fileSystem.Directory.Exists(folderPath))
            {
                _fileSystem.Directory.CreateDirectory(folderPath);
            }
        }
 
        public IFileInfo GetFileInfo(string filePath) => _fileSystem.FileInfo.FromFileName(filePath);

        public IEnumerable<string> GetFiles(string directoryPath, string extensionsToSearch, SearchOption searchOption)
        {
            string[] extensions = extensionsToSearch.Split(";");

            IEnumerable<string>? files = _fileSystem.Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

            return files.Where(e => extensions.Contains(Path.GetExtension(e).TrimStart('.').ToLowerInvariant()));
        }

        public byte[] GetFileInBytes(UploadFileBlockRequestModel UploadBlockMetaData)
        {
            IFileInfo fileInfo = _fileSystem.FileInfo.FromFileName(UploadBlockMetaData.FullFileName);

            byte[] byteData = new byte[UploadBlockMetaData.Length];

            using (Stream? fs = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                fs.Seek(UploadBlockMetaData.Offset, SeekOrigin.Begin);
                fs.Read(byteData);
            }
            return byteData;
        }

        public List<FileDetail> GetFileMD5(IEnumerable<string> fileNames)
        {
            List<FileDetail> fileDetails = new();

            foreach (string? fileName in fileNames)
            {
                IFileInfo fileInfo = _fileSystem.FileInfo.FromFileName(fileName);
                using Stream? fs = fileInfo.OpenRead();
                byte[]? fileMd5Hash = CommonHelper.CalculateMD5(fs);

                FileDetail fileDetail = new()
                {
                    FileName = fileInfo.Name,
                    Hash = Convert.ToBase64String(fileMd5Hash)
                };
                fileDetails.Add(fileDetail);
            }
            return fileDetails;
        }
    }
}
