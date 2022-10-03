using AvcsXmlCatalogueBuilder;
using FakeItEasy;
using UKHO.WeekNumberUtils;

namespace AvcsXmlCatalogueBuilderTests
{
    public class Tests
    {
        private CatalogueBuilder catBuilder;

        [SetUp]
        public void Setup()
        {
            catBuilder = new CatalogueBuilder(A.Fake<ICatalogProvider>(), A.Fake<IUnitsAndFoliosProvider>());
        }

        [Test]
        public void Test1()
        {
            Assert.Pass();
        }

        [Test]
        public async Task TestStuff()
        {
            var result = await catBuilder.BuildAsync();

            Assert.IsNotNull(result);
            Assert.IsNotEmpty(result);
            Assert.AreEqual("", result);
        }

        [Test]
        public async Task TestFileIdentifier()
        {
            var currentWeekNumber = WeekNumber.GetUKHOWeekFromDateTime(DateTime.UtcNow);
            var result = await catBuilder.BuildAsync();
            StringAssert.Contains(
                $"<MD_FileIdentifier>{currentWeekNumber.Year}{currentWeekNumber.Week:D2}1</MD_FileIdentifier>", result);
        }
    }
}