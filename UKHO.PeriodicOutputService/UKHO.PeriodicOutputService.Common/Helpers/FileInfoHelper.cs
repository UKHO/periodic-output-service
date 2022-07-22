using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class FileInfoHelper : IFileInfoHelper
    {
        public FileInfo GetFileInfo(string filePath)
        {
            return new FileInfo(filePath);
        }
    }
}
