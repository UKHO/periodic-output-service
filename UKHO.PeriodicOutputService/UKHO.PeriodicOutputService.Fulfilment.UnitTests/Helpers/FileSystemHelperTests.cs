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
        public void Does_ConvertStreamToByteArray_Returns_NotNull()
        {
            using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                byte[]? result = _fileSystemHelper.ConvertStreamToByteArray(test_Stream);

                Assert.That(result, Is.Not.Null);
            }
        }

        [Test]
        public void Does_CreateFileCopy_Executes_Successful()
        {
            Stream fakeStream = A.Fake<Stream>();

            using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                _fileSystemHelper.CreateFileCopy(Path.Combine(filePath, "test.zip"), test_Stream);

            }
        }
    }
}
