using UKHO.Torus.Enc.Core.EncCatalogue;
using Catalog031ReaderFactory = UKHO.PeriodicOutputService.Common.Helpers.Catalog031ReaderFactory;


namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class Catalog031ReaderFactoryTests
    {
        private Catalog031ReaderFactory _catalog031ReaderFactory;
        private readonly string _catalogFilePath = Path.Combine(Directory.GetCurrentDirectory(), "TestData", "Catalog.031");
        private byte[] _catalogFileBytes;

        [SetUp]
        public void SetUp()
        {
            _catalog031ReaderFactory = new Catalog031ReaderFactory();
            _catalogFileBytes = File.ReadAllBytes(_catalogFilePath);
        }

        [Test]
        public void WhenGivenValidByteArray_ThenCreateShouldReturnCatalog031ReaderInstance()
        {
            var catalog031Reader = _catalog031ReaderFactory.Create(_catalogFileBytes);

            Assert.That(catalog031Reader, Is.InstanceOf<ICatalog031Reader>());
        }

        [Test]
        public void WhenGivenNullByteArray_ThenCreateShouldThrowArgumentNullException()
        {
            Assert.Throws<NullReferenceException>(() => _catalog031ReaderFactory.Create(null));
        }
    }
}
