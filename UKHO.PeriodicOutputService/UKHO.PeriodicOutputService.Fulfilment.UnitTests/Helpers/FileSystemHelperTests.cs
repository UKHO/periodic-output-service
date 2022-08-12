using System.IO.Abstractions;
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

        private const string filePath = @"d:\Test";
        private const string fileName = "M01X01.zip";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
            _fakeZipHelper = A.Fake<IZipHelper>();

            _fileSystemHelper = new FileSystemHelper(_fakefileSystem, _fakeZipHelper);
        }


        [Test]
        public void Does_Constructor_Throws_ArgumentNullException_When_Paramter_Is_Null()
        {
            Assert.Throws<ArgumentNullException>(
               () => new FileSystemHelper(null, _fakeZipHelper))
               .ParamName
               .Should().Be("fileSystem");

            Assert.Throws<ArgumentNullException>(
              () => new FileSystemHelper(_fakefileSystem, null))
              .ParamName
              .Should().Be("zipHelper");
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
    }
}
