
using Microsoft.VisualStudio.Web.CodeGeneration;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly IZipHelper _zipHelper;
        private readonly IFileInfoHelper _fileInfoHelper;

        public FileSystemHelper(IFileSystem fileSystem, IZipHelper zipHelper, IFileInfoHelper fileInfoHelper)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _zipHelper = zipHelper ?? throw new ArgumentNullException(nameof(zipHelper));
            _fileInfoHelper = fileInfoHelper ?? throw new ArgumentNullException(nameof(fileInfoHelper));
        }

        public void CreateDirectory(string folderPath)
        {
            if (!_fileSystem.DirectoryExists(folderPath))
            {
                _fileSystem.CreateDirectory(folderPath);
            }
        }

        public byte[] ConvertStreamToByteArray(Stream input)
        {
            var ms = new MemoryStream();
            input.CopyTo(ms);
            return ms.ToArray();
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

        public void CreateZipFile(string sourcePath, string destinationFilePath, bool deleteSourceDirectory = false)
        {
            _zipHelper.CreateZipFile(sourcePath, destinationFilePath);

            if (deleteSourceDirectory && _fileSystem.DirectoryExists(sourcePath))
            {
                _fileSystem.RemoveDirectory(sourcePath, true);
            }
        }

        public void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory = false)
        {
            _zipHelper.ExtractZipFile(sourceArchiveFileName, destinationDirectoryName);

            if (deleteSourceDirectory && _fileSystem.DirectoryExists(sourceArchiveFileName))
            {
                _fileSystem.RemoveDirectory(sourceArchiveFileName, true);
            }
        }

        public FileInfo GetFileInfo(string filePath)
        {
            return _fileInfoHelper.GetFileInfo(filePath);
        }

        public IEnumerable<string> GetFiles(string directoryPath, string fileExtension)
        {
            return _fileSystem.EnumerateFiles(directoryPath, "*." + fileExtension, SearchOption.TopDirectoryOnly);
        }
    }
}
