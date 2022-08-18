using System.IO.Abstractions;
using UKHO.PeriodicOutputService.Common.Utility;

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
            if (!_fileSystem.Directory.Exists(folderPath))
            {
                _fileSystem.Directory.CreateDirectory(folderPath);
            }
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
