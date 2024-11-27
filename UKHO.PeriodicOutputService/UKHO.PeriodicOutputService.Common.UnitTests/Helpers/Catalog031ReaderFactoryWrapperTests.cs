using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.Torus.Enc.Core.EncCatalogue;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class Catalog031ReaderFactoryWrapperTests
    {
        private Catalog031ReaderFactoryWrapper _catalog031ReaderFactoryWrapper;
        private readonly string _catalogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Catalog.031");
        private byte[] _catalogFileBytes;

        [SetUp]
        public void SetUp()
        {
            _catalog031ReaderFactoryWrapper = new Catalog031ReaderFactoryWrapper();
            _catalogFileBytes = File.ReadAllBytes(_catalogFilePath);
        }

        [Test]
        public void WhenGivenValidByteArray_ThenCreateShouldReturnCatalog031ReaderInstance()
        {
            var catalog031Reader = _catalog031ReaderFactoryWrapper.Create(_catalogFileBytes);

            Assert.That(catalog031Reader, Is.InstanceOf<ICatalog031Reader>());
        }

        [Test]
        public void WhenGivenNullByteArray_ThenCreateShouldThrowArgumentNullException()
        {
            Assert.Throws<NullReferenceException>(() => _catalog031ReaderFactoryWrapper.Create(null));
        }
    }
}
