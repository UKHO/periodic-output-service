using System.IO.Abstractions;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using UKHO.PeriodicOutputService.Common.Models.Ess;
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
            IFileInfo fileInfo = _fileSystem.FileInfo.New(UploadBlockMetaData.FullFileName);

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
                using (var outputFileStream = new FileStream(filePath, FileMode.Append, FileAccess.Write))
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
                IFileInfo fileInfo = _fileSystem.FileInfo.New(fileName);
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

        public IFileInfo GetFileInfo(string filePath) => _fileSystem.FileInfo.New(filePath);

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

        public void CreateZipFile(string sourceDirectoryName, string destinationArchiveFileName, bool deleteOldArchive = false)
        {
            if (deleteOldArchive && _fileSystem.File.Exists(destinationArchiveFileName))
            {
                _fileSystem.File.Delete(destinationArchiveFileName);
            }

            _zipHelper.CreateZipFile(sourceDirectoryName, destinationArchiveFileName);
        }

        public void CreateIsoAndSha1(string targetPath, string directoryPath, string volumeIdentifier)
        {
            IEnumerable<string>? srcFiles = GetAllFiles(directoryPath, SearchOption.AllDirectories);

            _fileUtility.CreateISOImage(srcFiles, targetPath, directoryPath, volumeIdentifier);

            _fileUtility.CreateSha1File(targetPath);
        }

        public void CreateXmlFile(byte[] fileContent, string targetPath)
        {
            _fileUtility.CreateXmlFile(fileContent, targetPath);
        }

        public Task CreateXmlFromObject<T>(T obj, string filePath, string fileName)
        {
            var serializer = new XmlSerializer(typeof(T));
            var namespaces = new XmlSerializerNamespaces();
            namespaces.Add("xsi", "http://www.w3.org/2001/XMLSchema-instance");
                       
            using (var fileStream = _fileSystem.File.OpenWrite(Path.Combine(filePath, fileName)))
            {
                using (var xmlTextWriter = new XmlTextWriter(fileStream, Encoding.UTF8) { Formatting = Formatting.Indented })
                {
                    serializer.Serialize(xmlTextWriter, obj, namespaces);
                }
            }

           return Task.CompletedTask;
        }

        public void CreateTextFile(string filePath, string fileName, string content)
        {
            _fileSystem.File.AppendAllText(Path.Combine(filePath, fileName), content);
        }

        public IEnumerable<ProductVersion> GetProductVersionsFromDirectory(string sourcePath, string aioCellName)
        {
            string searchPath = $"ENC_ROOT/GB/{aioCellName}";
            string currentPath = Path.Combine(sourcePath, searchPath);

            List<ProductVersion> productVersions = new();

            if (!_fileSystem.Directory.Exists(currentPath))
            {
                return productVersions;
            }

            var aioFolder = _fileSystem.Directory.GetDirectories(currentPath, aioCellName, SearchOption.AllDirectories).ToList();

            if (aioFolder.Count == 0)
            {
                return productVersions;
            }

            var editionFolders = _fileSystem.Directory.GetDirectories(aioFolder[0]).Select(Path.GetFileName).ToList();

            foreach (var editionFolder in editionFolders)
            {
                ProductVersion productVersion = new();

                productVersion.ProductName = aioCellName;

                productVersion.EditionNumber = Convert.ToInt32(editionFolder);

                var updateNumberFolders = _fileSystem.Directory.GetDirectories(Path.Combine(currentPath, editionFolder));

                var maxDirectory = updateNumberFolders.Select(d => new { Path = d, Number = int.Parse(Path.GetFileName(d)) })
                                               .OrderByDescending(d => d.Number)
                                               .FirstOrDefault();

                productVersion.UpdateNumber = Convert.ToInt32(maxDirectory!.Number);

                productVersions.Add(productVersion);
            }

            return productVersions;
        }
    }
}
