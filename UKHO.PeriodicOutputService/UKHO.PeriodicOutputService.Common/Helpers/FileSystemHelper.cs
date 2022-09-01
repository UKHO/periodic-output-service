
using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.PeriodicOutputService.Common.Utilities;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly IZipHelper _zipHelper;
        private readonly IFileUtility _fileUtility;

        public FileSystemHelper(IFileSystem fileSystem, IZipHelper zipHelper, IFileUtility fileUtility)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _zipHelper = zipHelper ?? throw new ArgumentNullException(nameof(zipHelper));
            _fileUtility = fileUtility ?? throw new ArgumentNullException(nameof(fileUtility));
        }

        public void CreateDirectory(string folderPath)
        {
            if (_fileSystem.Directory.Exists(folderPath))
            {
                _fileSystem.Directory.Delete(folderPath, true);
            }
            _fileSystem.Directory.CreateDirectory(folderPath);
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

        public void CreateFileCopy(string filePath, Stream stream)
        {
            if (stream != null)
            {
                using (var outputFileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite))
                {
                    stream.CopyTo(outputFileStream);
                }
            }
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

        public IFileInfo GetFileInfo(string filePath) => _fileSystem.FileInfo.FromFileName(filePath);

        public IEnumerable<string> GetFiles(string directoryPath, string extensionsToSearch, SearchOption searchOption)
        {
            string[] extensions = extensionsToSearch.Split(";");

            IEnumerable<string>? files = _fileSystem.Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

            return files.Where(e => extensions.Contains(Path.GetExtension(e).TrimStart('.').ToLowerInvariant()));
        }

        public IEnumerable<string> GetAllFiles(string directoryPath, SearchOption searchOption) => _fileSystem.Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

        public void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory = false)
        {
            _zipHelper.ExtractZipFile(sourceArchiveFileName, destinationDirectoryName);

            if (deleteSourceDirectory && _fileSystem.Directory.Exists(sourceArchiveFileName))
            {
                _fileSystem.Directory.Delete(sourceArchiveFileName, true);
            }
        }


        public void CreateIsoAndSha1(string targetPath, string directoryPath)
        {
            IEnumerable<string>? srcFiles = GetAllFiles(directoryPath, SearchOption.AllDirectories);

            _fileUtility.CreateISOImage(srcFiles, targetPath, directoryPath);

            _fileUtility.CreateSha1File(targetPath);
        }
    }
}
