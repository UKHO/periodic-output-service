using System.IO.Abstractions;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Models.Fss.Request;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    public class FileSystemHelperTests
    {
        private FileSystemHelper _fileSystemHelper;
        private IFileSystem _fakefileSystem;
        private IFileInfo _fakeFileInfo;

        private const string filePath = @"d:\Test";
        private const string fileName = "M01X01.zip";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
            _fakeFileInfo = A.Fake<IFileInfo>();

            _fileSystemHelper = new FileSystemHelper(_fakefileSystem);
        }


        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null() => Assert.Throws<ArgumentNullException>(
               () => new FileSystemHelper(null))
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
            _fileSystemHelper.GetFiles(filePath, "*.zip", SearchOption.TopDirectoryOnly);

            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(filePath, "*.*", SearchOption.TopDirectoryOnly)).MustHaveHappenedOnceExactly();

        }

        [Test]
        public void Does_GetFileInBytes_Returns_Bytes()
        {
            UploadFileBlockRequestModel uploadFileBlockRequestModel = new()
            {
                BatchId = Guid.NewGuid().ToString(),
                BlockId = "Block_00001",
                FileName = "M01X01",
                FullFileName = filePath,
                Length = 100,
                Offset = 1
            };

            IEnumerable<string> fileNames = new List<string> { fileName };

            IFileInfo fileInfo = _fakefileSystem.FileInfo.FromFileName(fileName);
            A.CallTo(() => fileInfo.Name).Returns(fileName);

            A.CallTo(() => _fakefileSystem.FileInfo.FromFileName(A<string>.Ignored)).Returns(fileInfo);

            byte[]? result = _fileSystemHelper.GetFileInBytes(uploadFileBlockRequestModel);
        }
    }
}
