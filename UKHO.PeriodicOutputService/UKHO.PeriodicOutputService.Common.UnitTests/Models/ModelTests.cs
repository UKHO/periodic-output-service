using UKHO.PeriodicOutputService.Common.Models;
using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Models
{
    public class ModelTests
    {
        [Test]
        public void FormattedWeekNumber_ReturnsCorrectDetails()
        {
            var weekNumber = WeekNumber.GetUKHOWeekFromDateTime(new DateTime(2025, 1, 1));
            var formattedWeekNumber = new FormattedWeekNumber(weekNumber);

            Assert.Multiple(() =>
            {
                Assert.That(formattedWeekNumber.Week, Is.EqualTo("52"));
                Assert.That(formattedWeekNumber.Year, Is.EqualTo("2024"));
                Assert.That(formattedWeekNumber.YearWeek, Is.EqualTo("2024 / 52"));
                Assert.That(formattedWeekNumber.YearShort, Is.EqualTo("24"));
            });
        }
    }
}
