using System.IO.Abstractions;
using System.Text;
using FakeItEasy;
using FluentAssertions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Utility;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    public class FileSystemHelperTests
    {
        private FileSystemHelper _fileSystemHelper;
        private IFileSystem _fakefileSystem;
        private IZipHelper _fakeZipHelper;
        private IFileUtility _fakeFileUtility;

        private const string filePath = @"d:\Test";
        private const string fileName = "M01X01.zip";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
            _fakeZipHelper = A.Fake<IZipHelper>();
            _fakeFileUtility = A.Fake<IFileUtility>();

            _fileSystemHelper = new FileSystemHelper(_fakefileSystem, _fakeZipHelper, _fakeFileUtility);
        }


        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
               () => new FileSystemHelper(null, _fakeZipHelper, _fakeFileUtility))
               .ParamName
               .Should().Be("fileSystem");

            Assert.Throws<ArgumentNullException>(
              () => new FileSystemHelper(_fakefileSystem, null, _fakeFileUtility))
              .ParamName
              .Should().Be("zipHelper");

            Assert.Throws<ArgumentNullException>(
             () => new FileSystemHelper(_fakefileSystem, _fakeZipHelper, null))
             .ParamName
             .Should().Be("fileUtility");
        }

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
        public void Does_ExtractZipFile_Completed_When_DirectoryExists()
        {
            A.CallTo(() => _fakefileSystem.Directory.Exists(filePath)).Returns(true);

            _fileSystemHelper.ExtractZipFile(filePath, filePath, true);

            A.CallTo(() => _fakefileSystem.Directory.Delete(filePath, true))
                            .MustHaveHappenedOnceExactly();
        }


        [Test]
        public void Does_CreateFileCopy_Executes_Successful()
        {
            using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                _fileSystemHelper.CreateFileCopy(Path.Combine(filePath, "test.zip"), test_Stream);
            }
        }

        [Test]
        public void Does_CreateIsoAndSha1_Executes_Successfully()
        {
            A.CallTo(() => _fakefileSystem.Directory.EnumerateFiles(filePath, "*.*", SearchOption.AllDirectories)).Returns(new List<string> { "Test1", "Test2" });

            _fileSystemHelper.CreateIsoAndSha1(filePath, filePath);

            A.CallTo(() => _fakeFileUtility.CreateISOImage(A<IEnumerable<string>>.Ignored, A<string>.Ignored, A<string>.Ignored))
                           .MustHaveHappenedOnceExactly();


            A.CallTo(() => _fakeFileUtility.CreateSha1File(filePath))
                           .MustHaveHappenedOnceExactly();

        }
    }
}
