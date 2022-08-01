using System.IO.Abstractions;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using UKHO.PeriodicOutputService.Common.Helpers;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    public class FileSystemHelperTests
    {
        private FileSystemHelper _fileSystemHelper;
        private IFileSystem _fakefileSystem;
        private IZipHelper _fakeZipHelper;
        private IFileInfoHelper _fakeFileInfoHelper;
        private IFileInfo _fakeFileInfo;

        private const string filePath = @"d:\Test";
        private const string fileName = "M01X01.zip";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
            _fakeZipHelper = A.Fake<IZipHelper>();
            _fakeFileInfo = A.Fake<IFileInfo>();
            _fakeFileInfoHelper = A.Fake<IFileInfoHelper>();

            _fileSystemHelper = new FileSystemHelper(_fakefileSystem, _fakeZipHelper, _fakeFileInfoHelper);
        }


        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null() => Assert.Throws<ArgumentNullException>(
               () => new FileSystemHelper(null, _fakeZipHelper, _fakeFileInfoHelper))
               .ParamName
               .Should().Be("fileSystem");

        [Test]
        public void Does_CreateFolder_Completed_When_Directory_Exists()
        {

            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(filePath))
                            .MustNotHaveHappened();

        }

        [Test]
        public void Does_CreateFolder_Completed_When_Directory_Doesnot_Exists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(false);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.Directory.CreateDirectory(filePath))
                            .MustHaveHappenedOnceExactly();
        }

        [Test]
        public void Does_ConvertStreamToByteArray_Returns_NotNull()
        {
            using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                byte[]? result = _fileSystemHelper.ConvertStreamToByteArray(test_Stream);

                Assert.That(result, Is.Not.Null);
            }
        }

        [Test]
        public void Does_CreateZipFile_Completed_When_DirectoryExists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.CreateZipFile(filePath, filePath, true);

            A.CallTo(() => _fakefileSystem.Directory.Delete(filePath, true))
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
            List<Common.Models.Fss.Request.FileDetail> fileDetails = new();

            IEnumerable<string> fileNames = new List<string> { fileName };

            IFileInfo fileInfo = _fakefileSystem.FileInfo.FromFileName(fileName);
            A.CallTo(() => fileInfo.Name).Returns(fileName);

            A.CallTo(() => _fakefileSystem.FileInfo.FromFileName(A<string>.Ignored)).Returns(fileInfo);

            List<Common.Models.Fss.Request.FileDetail>? result = _fileSystemHelper.GetFileMD5(fileNames);

            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(1));
                Assert.That(result.FirstOrDefault().FileName, Is.EqualTo(fileName));
                Assert.That(result.FirstOrDefault().Hash, Is.Not.Null);
            });

        }

        [Test]
        public void Does_GetFiles_Executes_Successful()
        {
            IEnumerable<string>? result = _fileSystemHelper.GetFiles(filePath, "*.zip", SearchOption.TopDirectoryOnly);

            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(filePath, "*.*", SearchOption.TopDirectoryOnly)).MustHaveHappenedOnceExactly();

        }

        //[Test]
        //public void Does_CreateFileCopy_Executes_Successful()
        //{
        //    using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
        //    {
        //        _fileSystemHelper.CreateFileCopy(Path.Combine(filePath, "test.zip"), test_Stream);
        //    }
        //}
    }
}
