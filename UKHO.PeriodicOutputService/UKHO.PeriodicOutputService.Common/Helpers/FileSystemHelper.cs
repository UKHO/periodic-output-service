using System.IO.Abstractions;
using System.Security.Cryptography;
using DiscUtils.Iso9660;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class FileSystemHelper : IFileSystemHelper
    {
        private readonly IFileSystem _fileSystem;
        private readonly IZipHelper _zipHelper;

        public FileSystemHelper(IFileSystem fileSystem, IZipHelper zipHelper)
        {
            _fileSystem = fileSystem ?? throw new ArgumentNullException(nameof(fileSystem));
            _zipHelper = zipHelper ?? throw new ArgumentNullException(nameof(zipHelper));
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
            var iso = new CDBuilder
            {
                UseJoliet = true,
                VolumeIdentifier = "FullAVCSExchangeSet"
            };

            foreach (string? file in srcFiles)
            {
                var fi = new FileInfo(file);
                if (fi.Directory.Name == directoryPath)
                {
                    iso.AddFile($"{fi.Name}", fi.FullName);
                    continue;
                }
                string? srcDir = fi.Directory.FullName.Replace(directoryPath, "").TrimEnd('\\');
                iso.AddDirectory(srcDir);
                iso.AddFile($"{srcDir}\\{fi.Name}", fi.FullName);
            }
            iso.Build(targetPath);

            byte[] isoFileBytes = System.Text.Encoding.UTF8.GetBytes(targetPath);
            string hash = BitConverter.ToString(SHA1.Create().ComputeHash(isoFileBytes)).Replace("-", "");
            File.WriteAllText(targetPath + ".sha1", hash);
        }
    }
}
