using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    [ExcludeFromCodeCoverage]
    public class ZipHelper : IZipHelper
    {
        public void CreateZipFile(string sourcePath, string destinationPath)
        {
            ZipFile.CreateFromDirectory(sourcePath, destinationPath);
        }

        public void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName)
        {
            ZipFile.ExtractToDirectory(sourceArchiveFileName, destinationDirectoryName);
        }
    }
}
