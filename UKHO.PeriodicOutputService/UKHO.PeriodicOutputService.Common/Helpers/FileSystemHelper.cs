using Microsoft.VisualStudio.Web.CodeGeneration;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;

        public FileSystemHelper(IFileSystem fileSystem) => _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));

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
    }
}
