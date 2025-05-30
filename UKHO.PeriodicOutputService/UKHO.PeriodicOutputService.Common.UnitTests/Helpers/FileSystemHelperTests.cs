using System.Globalization;
using System.IO.Abstractions;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;
using UKHO.PeriodicOutputService.Common.Models.Pks;
using UKHO.PeriodicOutputService.Common.Utilities;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    public class FileSystemHelperTests
    {
        private FileSystemHelper _fileSystemHelper;
        private IFileSystem _fakefileSystem;
        private IZipHelper _fakeZipHelper;
        private IFileUtility _fakeFileUtility;
        private IFileInfo _fakeFileInfo;

        private const string filePath = @"d:\Test";
        private const string fileName = "M01X01.zip";
        private const string volumeIdentifier = "M01X01";

        private string content = "test";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
            _fakeZipHelper = A.Fake<IZipHelper>();
            _fakeFileUtility = A.Fake<IFileUtility>();
            _fakeFileInfo = A.Fake<IFileInfo>();

            _fileSystemHelper = new FileSystemHelper(_fakefileSystem, _fakeZipHelper, _fakeFileUtility);
        }

        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            var exception = Assert.Throws<ArgumentNullException>(
               () => new FileSystemHelper(null, _fakeZipHelper, _fakeFileUtility));
            Assert.That(exception.ParamName, Is.EqualTo("fileSystem"));

            exception = Assert.Throws<ArgumentNullException>(
              () => new FileSystemHelper(_fakefileSystem, null, _fakeFileUtility));
            Assert.That(exception.ParamName, Is.EqualTo("zipHelper"));

            exception = Assert.Throws<ArgumentNullException>(
             () => new FileSystemHelper(_fakefileSystem, _fakeZipHelper, null));
            Assert.That(exception.ParamName, Is.EqualTo("fileUtility"));
        }

        [Test]
        public void Does_CreateDirectory_DeleteAndCreate_Folder_When_Directory_Exists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.Directory.Delete(filePath, true))
                            .MustHaveHappened();
            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(filePath))
                            .MustHaveHappened();
        }

        [Test]
        public void Does_CreateFolder_Create_Folder_When_Directory_Doesnot_Exists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(false);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(filePath))
                            .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_ExtractZipFile_Completed_When_DirectoryExists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.ExtractZipFile(filePath, filePath, true);

            A.CallTo(() => _fakefileSystem.Directory.Delete(filePath, true))
                            .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_GetFileMD5_Returns_FileDetails_With_Hash()
        {
            IEnumerable<string> fileNames = new List<string> { fileName };

            var fileInfo = _fakefileSystem.FileInfo.New(fileName);
            A.CallTo(() => fileInfo.Name).Returns(fileName);
            A.CallTo(() => fileInfo.OpenRead()).Returns(new MockFileSystemStream(new MemoryStream(new byte[10]), "Test", default));
            A.CallTo(() => _fakefileSystem.FileInfo.New(A<string>.Ignored)).Returns(fileInfo);

            var result = _fileSystemHelper.GetFileMD5(fileNames);

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.FirstOrDefault().FileName, Is.EqualTo(fileName));
                Assert.That(result.FirstOrDefault().Hash, Is.Not.Null);
            });

        }

        [Test]
        public void Does_GetFiles_Call_EnumerateFiles_To_Get_Directory_Files()
        {
            _fileSystemHelper.GetFiles(filePath, "*.zip", SearchOption.TopDirectoryOnly);

            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(filePath, "*.*", SearchOption.TopDirectoryOnly)).MustHaveHappenedOnceExactly();
        }


        [Test]
        public void Does_CreateIsoAndSha1_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories)).Returns(new List<string> { "Test1", "Test2" });

            _fileSystemHelper.CreateIsoAndSha1(filePath, filePath, volumeIdentifier);

            A.CallTo(() => _fakeFileUtility.CreateISOImage(A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored))
                           .MustHaveHappenedOnceExactly();


            A.CallTo(() => _fakeFileUtility.CreateSha1File(filePath))
                           .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateXmlFile_Executes_Successfully()
        {
            var byteContent = new byte[100];

            _fileSystemHelper.CreateXmlFile(byteContent, filePath);

            A.CallTo(() => _fakeFileUtility.CreateXmlFile(A<byte[]>.Ignored, A<string>.Ignored))
                           .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenValidPermitXmlDataPassed_ThenCreateXmlFromObject_Executes_Successfully()
        {
            PksXml pKSXml = new()
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Cellkeys = new()
                {
                    ProductKeyServiceResponses = new List<ProductKeyServiceResponse>()
                    {
                        new()
                        {
                            Edition = "10",
                            ProductName = "test",
                            Key = "12345"
                        }
                    },
                }
            };

            A.CallTo(() => _fakefileSystem.File.OpenWrite(A<string>.Ignored))
                           .Returns(new MockFileSystemStream(new MemoryStream(), "C:\\Test.xml", false));

            var result = _fileSystemHelper.CreateXmlFromObject(pKSXml, "C:\\Test", "test.txt");

            Assert.That(result, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void WhenValidDataPassed_ThenCreateTextFile_Executes_Successfully()
        {
            _fileSystemHelper.CreateTextFile("C:\\Test", "test.txt", "test");

            A.CallTo(() => _fakefileSystem.File.AppendAllText(A<string>.Ignored, A<string>.Ignored))
                           .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_GetFileInBytes_Returns_Bytes_Passing_Stream()
        {
            Stream stream = new MemoryStream(new byte[10]);


            A.CallTo(() => _fakefileSystem.FileInfo.New(A<string>.Ignored))
                            .Returns(_fakeFileInfo);

            A.CallTo(() => _fakeFileInfo.Open(A<FileMode>.Ignored, A<FileAccess>.Ignored, A<FileShare>.Ignored)).Returns(new MockFileSystemStream(stream, "Test", default));

            var result = _fileSystemHelper.GetFileInBytes(GetUploadFileBlockRequestModel());

            Assert.That(result, Has.Length.EqualTo(100000));
        }

        [Test]
        public void Does_GetFileInfo_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.FileInfo.New(filePath)).Returns(_fakeFileInfo);
            A.CallTo(() => _fakeFileInfo.Name).Returns(fileName);

            var result = _fileSystemHelper.GetFileInfo(filePath);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.SameAs(_fakeFileInfo));
            });
        }

        private static UploadFileBlockRequestModel GetUploadFileBlockRequestModel() => new()
        {
            FileName = fileName,
            FullFileName = Path.Combine(filePath, fileName),
            Length = 100000
        };

        [Test]
        public void Does_ReadFileText_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.File.Exists(filePath)).Returns(true);

            _fileSystemHelper.ReadFileText(filePath);

            A.CallTo(() => _fakefileSystem.File.ReadAllText(filePath))
                .MustHaveHappened();

        }

        [Test]
        public void Does_CreateFileContent_Executes_Successfully_WhenContentIsPresent()
        {
            A.CallTo(() => _fakefileSystem.File.Exists(filePath)).Returns(true);

            _fileSystemHelper.CreateFileContent(filePath, content);

            A.CallTo(() => _fakefileSystem.File.WriteAllText(filePath, content))
                .MustHaveHappened();
        }

        [Test]
        public void Does_CreateFileContent_Execution_Fails_WhenContentIsNotPresent()
        {
            content = "";

            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.CreateFileContent(filePath, content);

            A.CallTo(() => _fakefileSystem.File.WriteAllText(filePath, content))
                .MustNotHaveHappened();
        }

        [Test]
        public void Does_DeleteFile_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.File.Exists(filePath)).Returns(true);

            _fileSystemHelper.DeleteFile(filePath);

            A.CallTo(() => _fakefileSystem.File.Delete(filePath))
                .MustHaveHappened();

        }

        [Test]
        public void Does_DeleteFolder_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.DeleteFolder(filePath);

            A.CallTo(() => _fakefileSystem.Directory.Delete(filePath))
                .MustHaveHappened();

        }

        [Test]
        public void Does_GetProductVersionsFromDirectory_Returns_ProductVersions_When_Directories_Exist()
        {
            // Arrange
            var sourcePath = @"C:\TestSource";
            var currentPath = Path.Combine(sourcePath, "ENC_ROOT");

            var countryFolder = Path.Combine(currentPath, "Country1");
            var encFolder = Path.Combine(countryFolder, "ENC1");
            var editionFolder = Path.Combine(encFolder, "001");
            var updateFolder1 = Path.Combine(editionFolder, "01");
            var updateFolder2 = Path.Combine(editionFolder, "02");

            A.CallTo(() => _fakefileSystem.Directory.Exists(currentPath)).Returns(true);
            A.CallTo(() => _fakefileSystem.Directory.GetDirectories(currentPath, "*", SearchOption.TopDirectoryOnly))
                .Returns(new[] { countryFolder });
            A.CallTo(() => _fakefileSystem.Directory.GetDirectories(countryFolder, "*", SearchOption.TopDirectoryOnly))
                .Returns(new[] { encFolder });
            A.CallTo(() => _fakefileSystem.Directory.GetDirectories(encFolder))
                .Returns(new[] { editionFolder });
            A.CallTo(() => _fakefileSystem.Directory.GetDirectories(editionFolder))
                .Returns(new[] { updateFolder1, updateFolder2 });

            // Act
            var result = _fileSystemHelper.GetProductVersionsFromDirectory(sourcePath);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result.Count(), Is.EqualTo(1));
                Assert.That(result.First().ProductName, Is.EqualTo("ENC1"));
                Assert.That(result.First().EditionNumber, Is.EqualTo(1));
                Assert.That(result.First().UpdateNumber, Is.EqualTo(2));
            });
        }

        [Test]
        public void Does_GetProductVersionsFromDirectory_Returns_Empty_When_No_Directories_Exist()
        {
            // Arrange
            var sourcePath = @"C:\TestSource";
            var currentPath = Path.Combine(sourcePath, "ENC_ROOT");

            A.CallTo(() => _fakefileSystem.Directory.Exists(currentPath)).Returns(false);

            // Act
            var result = _fileSystemHelper.GetProductVersionsFromDirectory(sourcePath);

            // Assert
            Assert.That(result, Is.Empty);
        }
    }

    public class MockFileSystemStream : FileSystemStream
    {
        public MockFileSystemStream(Stream stream, string path, bool isAsync) : base(stream, path, isAsync)
        {
        }
    }
}
