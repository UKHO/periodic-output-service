using System.IO.Abstractions;

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
    }
}
