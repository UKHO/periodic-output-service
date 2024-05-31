using System.Globalization;
using FakeItEasy;
using FluentAssertions;
using UKHO.PeriodicOutputService.Common.Helpers;
using UKHO.PeriodicOutputService.Common.Providers;
using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.UnitTests.Helpers
{
    [TestFixture]
    public class MacroTransformerTests
    {
        private MacroTransformer? macroTransformer;
        private ICurrentDateTimeProvider? currentDateTimeProvider;

        [SetUp]
        public void Setup()
        {
            currentDateTimeProvider = A.Fake<CurrentDateTimeProvider>();

            macroTransformer = new MacroTransformer(currentDateTimeProvider);
        }


        private string? MacroTest(string macroExpression, string output)
        {
            var result = macroTransformer.ExpandMacros(macroExpression);
            if (macroExpression == "$(now.AddDays(1))" | macroExpression == "$(now.AddDays(-1))" | macroExpression == "$(now)") // because of difference in time in seconds hence this
            {
                result.Substring(0, result.IndexOf("")).Should().Be(output.Substring(0, output.IndexOf("")));
                return result;
            }

            return result;
        }


        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisYearLast2Digits()
        {
            string macroExpression = "$(now.Year2)";
            string output = DateTime.Now.Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisYear()
        {
            string macroExpression = "$(now.Year)";
            string output = DateTime.Now.Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisMonth()
        {
            string macroExpression = "$(now.Month)";
            string output = DateTime.Now.Month.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisMonthLast2Digits()
        {
            string macroExpression = "$(now.Month2)";
            string output = DateTime.Now.Month.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisMonthMonthName()
        {
            string macroExpression = "$(now.MonthShortName)";
            string output = DateTime.Now.ToString("MMMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisMonthMonthNameShort()
        {
            string macroExpression = "$(now.MonthName)";
            string output = DateTime.Now.ToString("MMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedToday()
        {
            string macroExpression = "$(now.Day)";
            string output = DateTime.Now.Day.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedTodayPadded()
        {
            string macroExpression = "$(now.Day2)";
            string output = DateTime.Now.Day.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedTodayName()
        {
            string macroExpression = "$(now.DayName)";
            string output = DateTime.Now.ToString("dddd");

            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedTodayShortName()
        {
            string macroExpression = "$(now.DayShortName)";
            string output = DateTime.Now.ToString("ddd");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAddYear()
        {
            string macroExpression = "$(now.AddDays(365).Year)";
            string output = DateTime.Now.AddDays(365).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAddYearGetLast2Digits()
        {
            string macroExpression = "$(now.AddDays(365).Year2)";
            string output = DateTime.Now.AddDays(365).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1Day()
        {
            string macroExpression = "$(now.AddDays(1).Day)";
            string output = DateTime.Now.AddDays(1).Day.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayPaddedOutput()
        {
            string macroExpression = "$(now.AddDays(1).Day2)";
            string output = DateTime.Now.AddDays(1).Day.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputDayName()
        {
            string macroExpression = "$(now.AddDays(1).DayName)";
            string output = DateTime.Now.AddDays(1).ToString("dddd");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputDayShortName()
        {
            string macroExpression = "$(now.AddDays(1).DayShortName)";
            string output = DateTime.Now.AddDays(1).ToString("ddd");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputWeekNumber()
        {
            string macroExpression = "$(now.AddDays(1).WeekNumber)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(1)).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputWeekNumberPadded()
        {
            string macroExpression = "$(now.AddDays(1).WeekNumber)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(1)).Week.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputWeekNumberAsYear()
        {
            string macroExpression = "$(now.AddDays(1).WeekNumber.Year)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(1)).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputWeekNumberAsYearLast2Digits()
        {
            string macroExpression = "$(now.AddDays(1).WeekNumber.Year2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(1)).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputMonth()
        {
            string macroExpression = "$(now.AddDays(1).Month)";
            string output = DateTime.Now.AddDays(1).Month.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputMonthPadded()
        {
            string macroExpression = "$(now.AddDays(1).Month2)";
            string output = DateTime.Now.AddDays(1).Month.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputMonthName()
        {
            string macroExpression = "$(now.AddDays(1).MonthName)";
            string output = DateTime.Now.AddDays(1).ToString("MMMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputMonthShortName()
        {
            string macroExpression = "$(now.AddDays(1).MonthShortName)";
            string output = DateTime.Now.AddDays(1).ToString("MMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputDay()
        {
            string macroExpression = "$(now.AddDays(-1).Day)";
            string output = DateTime.Now.AddDays(-1).Day.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputDayPadded()
        {
            string macroExpression = "$(now.AddDays(-1).Day2)";
            string output = DateTime.Now.AddDays(-1).Day.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputDayName()
        {
            string macroExpression = "$(now.AddDays(-1).DayName)";
            string output = DateTime.Now.AddDays(-1).ToString("dddd");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputDayShortName()
        {
            string macroExpression = "$(now.AddDays(-1).DayShortName)";
            string output = DateTime.Now.AddDays(-1).ToString("ddd");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputWeekNumber()
        {
            string macroExpression = "$(now.AddDays(-1).WeekNumber)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1)).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputWeekNumberPadded()
        {
            string macroExpression = "$(now.AddDays(-1).WeekNumber2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1)).Week.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputYearNumber()
        {
            string macroExpression = "$(now.AddDays(-1).Year)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1)).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputYearNumberLast2Digits()
        {
            string macroExpression = "$(now.AddDays(-1).Year2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1)).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputMonth()
        {
            string macroExpression = "$(now.AddDays(-1).Month)";
            string output = DateTime.Now.AddDays(-1).Month.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputMonthPadded()
        {
            string macroExpression = "$(now.AddDays(-1).Month2)";
            string output = DateTime.Now.AddDays(-1).Month.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputMonthName()
        {
            string macroExpression = "$(now.AddDays(-1).MonthName)";
            string output = DateTime.Now.AddDays(-1).ToString("MMMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputMonthShortName()
        {
            string macroExpression = "$(now.AddDays(-1).MonthShortName)";
            string output = DateTime.Now.AddDays(-1).ToString("MMM");
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputMonthNumber()
        {
            string macroExpression = "$(now.WeekNumber -1)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1 * 7)).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1WeekOutputWeek()
        {
            string macroExpression = "$(now.WeekNumber -1)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1 * 7)).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1WeekOutputWeekPadded()
        {
            string macroExpression = "$(now.WeekNumber2 -1)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-1 * 7)).Week.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus200WeeksOutputYear()
        {
            string macroExpression = "$(now.WeekNumber -200.Year)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-200 * 7)).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus200WeeksOutputYearLast2Digits()
        {
            string macroExpression = "$(now.WeekNumber - 200.Year2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(-200 * 7)).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedMinus1DayOutputDate()
        {
            string macroExpression = "$(now.AddDays(-1))";
            string output = DateTime.Now.AddDays(-1).ToUniversalTime().ToString(CultureInfo.InvariantCulture);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedAdd1DayOutputDate()
        {
            string macroExpression = "$(now.AddDays(1))";
            string output = DateTime.Now.AddDays(1).ToUniversalTime().ToString(CultureInfo.InvariantCulture);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedTodayDayOutputDate()
        {
            string macroExpression = "$(now)";
            string output = DateTime.Now.ToUniversalTime().ToString(CultureInfo.InvariantCulture);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisWeeksNumber()
        {
            string macroExpression = "$(now.WeekNumber)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisWeeksNumberPadded()
        {
            string macroExpression = "$(now.WeekNumber2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Week.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisYearAsWeekNumber()
        {
            string macroExpression = "$(now.Year)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisYearAsWeekNumberLast2Digits()
        {
            string macroExpression = "$(now.Year2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedNextWeekNumber()
        {
            string macroExpression = "$(now.WeekNumber + 1)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(1 * 7)).Week.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedNext200WeeksOutputYear()
        {
            string macroExpression = "$(now.WeekNumber + 200.Year)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(200 * 7)).Year.ToString();
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedNext200WeeksOutputYeaLast2Digits()
        {
            string macroExpression = "$(now.WeekNumber + 200.Year2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now.AddDays(200 * 7)).Year.ToString().Substring(2, 2);
            MacroTest(macroExpression, output).Should().Be(output);
        }

        [Test]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturnedThisYearWeekNumberDividedByWeekNumberPadded()
        {
            string macroExpression = "$(now.WeekNumber.Year)/$(now.WeekNumber2)";
            string output = WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Year + "/" + WeekNumber.GetUKHOWeekFromDateTime(DateTime.Now).Week.ToString().PadLeft(2, '0');
            MacroTest(macroExpression, output).Should().Be(output);
        }

        public static object[] unavailableMacroValues =
        {

        };

        [Test]
        public void WhenMacroValueIsUnavailable_ThenSameValueIsReturned()
        {
            var sameValueTests = new List<(string, string)>
            {
                ( "$(now.Year3)", "$(now.Year3)" ), //unavailable or invalid
                ( "$(23)", "$(23)" ),
                ( "$(now.Mont)", "$(now.Mont)" ), //spelling mistake
                ( null, null )!,
                ("", "" )
            };

            foreach (var test in sameValueTests)
            {
                var result = macroTransformer.ExpandMacros(test.Item1);
                result.Should().Be(test.Item2);
            }
        }
    }
}
