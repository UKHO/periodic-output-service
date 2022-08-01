using System.IO.Abstractions;
using System.Security.Cryptography;
using DiscUtils.Iso9660;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

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
            if (!_fileSystem.Directory.Exists(folderPath))
            {
                _fileSystem.Directory.CreateDirectory(folderPath);
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

            if (deleteSourceDirectory && _fileSystem.Directory.Exists(sourcePath))
            {
                _fileSystem.Directory.Delete(sourcePath, true);
            }
        }

        public void ExtractZipFile(string sourceArchiveFileName, string destinationDirectoryName, bool deleteSourceDirectory = false)
        {
            _zipHelper.ExtractZipFile(sourceArchiveFileName, destinationDirectoryName);

            if (deleteSourceDirectory && _fileSystem.Directory.Exists(sourceArchiveFileName))
            {
                _fileSystem.Directory.Delete(sourceArchiveFileName, true);
            }
        }

        public IFileInfo GetFileInfo(string filePath) => _fileInfoHelper.GetFileInfo(filePath);

        public IEnumerable<string> GetFiles(string directoryPath, string extensionsToSearch, SearchOption searchOption)
        {
            string[] extensions = extensionsToSearch.Split(";");

            IEnumerable<string>? files = _fileSystem.Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

            return files.Where(e => extensions.Contains(Path.GetExtension(e).TrimStart('.').ToLowerInvariant()));
        }

        public IEnumerable<string> GetAllFiles(string directoryPath, SearchOption searchOption) => _fileSystem.Directory.EnumerateFiles(directoryPath, "*.*", searchOption);

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
