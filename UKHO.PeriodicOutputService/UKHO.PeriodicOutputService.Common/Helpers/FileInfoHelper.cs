using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]

    public class FileInfoHelper : IFileInfoHelper
    {
        private readonly IFileSystem _fileSystem;

        public FileInfoHelper(IFileSystem fileSystem) => _fileSystem = fileSystem;

        public IFileInfo GetFileInfo(string filePath) => _fileSystem.FileInfo.FromFileName(filePath);
    }
}
