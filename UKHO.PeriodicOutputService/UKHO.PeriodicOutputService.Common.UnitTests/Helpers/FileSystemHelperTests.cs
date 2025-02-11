using System.Globalization;
using System.IO.Abstractions;
using FakeItEasy;
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
        private const string FilePath = @"d:\Test";
        private const string FileName = "M01X01.zip";
        private const string VolumeIdentifier = "M01X01";

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
            var exception = Assert.Throws<ArgumentNullException>(() => new FileSystemHelper(null, _fakeZipHelper, _fakeFileUtility));
            Assert.That(exception.ParamName, Is.EqualTo("fileSystem"));

            exception = Assert.Throws<ArgumentNullException>(() => new FileSystemHelper(_fakefileSystem, null, _fakeFileUtility));
            Assert.That(exception.ParamName, Is.EqualTo("zipHelper"));

            exception = Assert.Throws<ArgumentNullException>(() => new FileSystemHelper(_fakefileSystem, _fakeZipHelper, null));
            Assert.That(exception.ParamName, Is.EqualTo("fileUtility"));
        }

        [Test]
        public void Does_CreateDirectory_DeleteAndCreate_Folder_When_Directory_Exists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(FilePath)).Returns(true);

            _fileSystemHelper.CreateDirectory(FilePath);

            A.CallTo(() => _fakefileSystem.Directory.Delete(FilePath, true)).MustHaveHappened();
            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(FilePath)).MustHaveHappened();
        }

        [Test]
        public void Does_CreateFolder_Create_Folder_When_Directory_Doesnot_Exists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(FilePath)).Returns(false);

            _fileSystemHelper.CreateDirectory(FilePath);

            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(FilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_ExtractZipFile_Completed_When_DirectoryExists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(FilePath)).Returns(true);

            _fileSystemHelper.ExtractZipFile(FilePath, FilePath, true);

            A.CallTo(() => _fakefileSystem.Directory.Delete(FilePath, true)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_GetFileMD5_Returns_FileDetails_With_Hash()
        {
            var fileNames = new List<string> { FileName };
            var fileInfo = _fakefileSystem.FileInfo.New(FileName);
            A.CallTo(() => fileInfo.Name).Returns(FileName);
            A.CallTo(() => fileInfo.OpenRead()).Returns(new MockFileSystemStream(new MemoryStream(new byte[10]), "Test", default));
            A.CallTo(() => _fakefileSystem.FileInfo.New(A<string>.Ignored)).Returns(fileInfo);

            var result = _fileSystemHelper.GetFileMD5(fileNames);

            Assert.Multiple(() =>
            {
                Assert.That(result, Has.Count.EqualTo(1));
                Assert.That(result.FirstOrDefault().FileName, Is.EqualTo(FileName));
                Assert.That(result.FirstOrDefault().Hash, Is.Not.Null);
            });
        }

        [Test]
        public void Does_GetFiles_Call_EnumerateFiles_To_Get_Directory_Files()
        {
            _fileSystemHelper.GetFiles(FilePath, "*.zip", SearchOption.TopDirectoryOnly);

            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(FilePath, "*.*", SearchOption.TopDirectoryOnly)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateIsoAndSha1_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(FilePath, "*.*", SearchOption.AllDirectories)).Returns(new List<string> { "Test1", "Test2" });

            _fileSystemHelper.CreateIsoAndSha1(FilePath, FilePath, VolumeIdentifier);

            A.CallTo(() => _fakeFileUtility.CreateISOImage(A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
            A.CallTo(() => _fakeFileUtility.CreateSha1File(FilePath)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_CreateXmlFile_Executes_Successfully()
        {
            var byteContent = new byte[100];

            _fileSystemHelper.CreateXmlFile(byteContent, FilePath);

            A.CallTo(() => _fakeFileUtility.CreateXmlFile(A<byte[]>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void WhenValidPermitXmlDataPassed_ThenCreateXmlFromObject_Executes_Successfully()
        {
            var pksXml = new PksXml
            {
                Date = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
                Cellkeys = new()
                {
                    ProductKeyServiceResponses =
                    [
                        new()
                        {
                            Edition = "10",
                            ProductName = "test",
                            Key = "12345"
                        }
                    ],
                }
            };

            A.CallTo(() => _fakefileSystem.File.OpenWrite(A<string>.Ignored)).Returns(new MockFileSystemStream(new MemoryStream(), "C:\\Test.xml", false));

            var result = _fileSystemHelper.CreateXmlFromObject(pksXml, "C:\\Test", "test.txt");

            Assert.That(result, Is.EqualTo(Task.CompletedTask));
        }

        [Test]
        public void WhenValidDataPassed_ThenCreateTextFile_Executes_Successfully()
        {
            _fileSystemHelper.CreateTextFile("C:\\Test", "test.txt", "test");

            A.CallTo(() => _fakefileSystem.File.AppendAllText(A<string>.Ignored, A<string>.Ignored)).MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_GetFileInBytes_Returns_Bytes_Passing_Stream()
        {
            Stream stream = new MemoryStream(new byte[10]);
            A.CallTo(() => _fakefileSystem.FileInfo.New(A<string>.Ignored)).Returns(_fakeFileInfo);
            A.CallTo(() => _fakeFileInfo.Open(A<FileMode>.Ignored, A<FileAccess>.Ignored, A<FileShare>.Ignored)).Returns(new MockFileSystemStream(stream, "Test", default));

            var result = _fileSystemHelper.GetFileInBytes(GetUploadFileBlockRequestModel());

            Assert.That(result, Has.Length.EqualTo(100000));
        }

        [Test]
        public void Does_GetFileInfo_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.FileInfo.New(FilePath)).Returns(_fakeFileInfo);
            A.CallTo(() => _fakeFileInfo.Name).Returns(FileName);

            var result = _fileSystemHelper.GetFileInfo(FilePath);

            Assert.Multiple(() =>
            {
                Assert.That(result, Is.Not.Null);
                Assert.That(result, Is.SameAs(_fakeFileInfo));
            });
        }

        private static UploadFileBlockRequestModel GetUploadFileBlockRequestModel() => new()
        {
            FileName = FileName,
            FullFileName = Path.Combine(FilePath, FileName),
            Length = 100000
        };

        [Test]
        public void Does_ReadFileText_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.File.Exists(FilePath)).Returns(true);

            _fileSystemHelper.ReadFileText(FilePath);

            A.CallTo(() => _fakefileSystem.File.ReadAllText(FilePath)).MustHaveHappened();
        }

        [Test]
        public void Does_CreateFileContent_Executes_Successfully_WhenContentIsPresent()
        {
            const string content = "test";
            A.CallTo(() => _fakefileSystem.File.Exists(FilePath)).Returns(true);

            _fileSystemHelper.CreateFileContent(FilePath, content);

            A.CallTo(() => _fakefileSystem.File.WriteAllText(FilePath, content)).MustHaveHappened();
        }

        [Test]
        public void Does_CreateFileContent_Execution_Fails_WhenContentIsNotPresent()
        {
            const string content = "";
            A.CallTo(() => _fakefileSystem.Directory.Exists(FilePath)).Returns(true);

            _fileSystemHelper.CreateFileContent(FilePath, content);

            A.CallTo(() => _fakefileSystem.File.WriteAllText(FilePath, content)).MustNotHaveHappened();
        }

        [Test]
        public void Does_DeleteFile_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.File.Exists(FilePath)).Returns(true);

            _fileSystemHelper.DeleteFile(FilePath);

            A.CallTo(() => _fakefileSystem.File.Delete(FilePath)).MustHaveHappened();
        }

        [Test]
        public void Does_DeleteFolder_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(FilePath)).Returns(true);

            _fileSystemHelper.DeleteFolder(FilePath);

            A.CallTo(() => _fakefileSystem.Directory.Delete(FilePath)).MustHaveHappened();
        }
    }

    public class MockFileSystemStream(Stream stream, string path, bool isAsync) : FileSystemStream(stream, path, isAsync) { }
}
