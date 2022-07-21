using System.Text;
using FakeItEasy;
using FluentAssertions;
using Microsoft.VisualStudio.Web.CodeGeneration;
using UKHO.PeriodicOutputService.Common.Helpers;

namespace UKHO.PeriodicOutputService.Fulfilment.UnitTests.Helpers
{
    public class FileSystemHelperTests
    {
        private FileSystemHelper _fileSystemHelper;
        private IFileSystem _fakefileSystem;

        private const string filePath = @"d:\Test";

        [SetUp]
        public void Setup()
        {
            _fakefileSystem = A.Fake<IFileSystem>();
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

            A.CallTo(() => _fakefileSystem.DirectoryExists(filePath)).Returns(true);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.CreateDirectory(filePath))
                            .MustNotHaveHappened();

        }

        [Test]
        public void Does_CreateFolder_Completed_When_Directory_Doesnot_Exists()
        {
            A.CallTo(() => _fakefileSystem.DirectoryExists(filePath)).Returns(false);

            _fileSystemHelper.CreateDirectory(filePath);

            A.CallTo(() => _fakefileSystem.CreateDirectory(filePath))
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
            using (var test_Stream = new MemoryStream(Encoding.UTF8.GetBytes("whatever")))
            {
                _fileSystemHelper.CreateFileCopy(Path.Combine(filePath, "test.zip"), test_Stream);
            }
        }
    }
}
