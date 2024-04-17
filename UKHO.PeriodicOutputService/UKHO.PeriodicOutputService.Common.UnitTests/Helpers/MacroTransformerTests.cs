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
        private MacroTransformer macroTransformer;
        private ICurrentDateTimeProvider currentDateTimeProvider;
        private static DateTime today = DateTime.Now;
        public static object[] macroValues =
        {
           new string[2]{ "$(now.Year)", today.Year.ToString() },
           new string[2]{ "$(now.Year2)", today.Year.ToString().Substring(2,2) },
           new string[2]{ "$(now.Month)", today.Month.ToString() },
           new string[2]{ "$(now.Month2)", today.Month.ToString().PadLeft(2,'0') },
           new string[2]{ "$(now.MonthName)", today.ToString("MMMM") },
           new string[2]{ "$(now.MonthShortName)", today.ToString("MMM") },
           new string[2]{ "$(now.Day)", today.Day.ToString() },
           new string[2]{ "$(now.Day2)", today.Day.ToString().PadLeft(2,'0') },
           new string[2]{ "$(now.DayName)", today.ToString("dddd") },
           new string[2]{ "$(now.DayShortName)", today.ToString("ddd") },
           new string[2]{ "$(now.AddDays(365).Year)", today.AddDays(365).Year.ToString() },
           new string[2]{ "$(now.AddDays(365).Year2)", today.AddDays(365).Year.ToString().Substring(2,2) },
           new string[2]{ "$(now.AddDays(1).Day)", today.AddDays(1).Day.ToString() },
           new string[2]{ "$(now.AddDays(1).Day2)", today.AddDays(1).Day.ToString().PadLeft(2,'0') },
           new string[2]{ "$(now.AddDays(1).DayName)", today.AddDays(1).ToString("dddd") },
           new string[2]{ "$(now.AddDays(1).DayShortName)", today.AddDays(1).ToString("ddd") },
           new string[2]{ "$(now.AddDays(1).WeekNumber)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1)).Week.ToString() },
           new string[2]{ "$(now.AddDays(1).WeekNumber2)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1)).Week.ToString().PadLeft(2,'0') },
           new string[2]{ "$(now.AddDays(1).WeekNumber.Year)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1)).Year.ToString() },
           new string[2]{ "$(now.AddDays(1).WeekNumber.Year2)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1)).Year.ToString().Substring(2,2) },
           new string[2]{ "$(now.AddDays(1).Month)", today.AddDays(1).Month.ToString() },
           new string[2]{ "$(now.AddDays(1).Month2)", today.AddDays(1).Month.ToString().PadLeft(2,'0') },
           new string[2]{ "$(now.AddDays(1).MonthName)", today.ToString("MMMM")},
           new string[2]{ "$(now.AddDays(1).MonthShortName)", today.ToString("MMM")},
           new string[2]{ "$(now.WeekNumber)", WeekNumber.GetUKHOWeekFromDateTime(today).Week.ToString()},
           new string[2]{ "$(now.WeekNumber2)", WeekNumber.GetUKHOWeekFromDateTime(today).Week.ToString().PadLeft(2,'0')},
           new string[2]{ "$(now.WeekNumber.Year)", WeekNumber.GetUKHOWeekFromDateTime(today).Year.ToString()},
           new string[2]{ "$(now.WeekNumber.Year2)", WeekNumber.GetUKHOWeekFromDateTime(today).Year.ToString().Substring(2,2)},
           new string[2]{ "$(now.WeekNumber +1)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1 * 7)).Week.ToString()},
           new string[2]{ "$(now.WeekNumber2 +1)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1 * 7)).Week.ToString().PadLeft(2,'0')},
           new string[2]{ "$(now.WeekNumber +1.Year)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1 * 7)).Year.ToString()},
           new string[2]{ "$(now.WeekNumber +1.Year2)", WeekNumber.GetUKHOWeekFromDateTime(today.AddDays(1 * 7)).Year.ToString().Substring(2,2)},
           new string[2]{ "$(now.AddDays(1))", today.ToUniversalTime().AddDays(1).ToString(CultureInfo. InvariantCulture) },
           new string[2]{ "$(now)", today.ToUniversalTime().ToString(CultureInfo.InvariantCulture)}
        };

        public static object[] unavailableMacroValues =
        {
           new string[2]{ "$(now.Year3)", "$(now.Year3)" }, //unavailable or invalid
           new string[2]{ "$(23)", "$(23)" },
           new string[2]{ "$(now.Mont)", "$(now.Mont)" }, //spelling mistake
           new string[2]{ null , null },
           new string[2]{ "" , "" }
        };

        [SetUp]
        public void Setup()
        {
            currentDateTimeProvider = A.Fake<CurrentDateTimeProvider>();

            macroTransformer = new MacroTransformer(currentDateTimeProvider);
        }

        [Test]
        [TestCaseSource(nameof(macroValues))]
        public void WhenMacroValueIsAvailable_ThenCorrespondingDateStringIsReturned(string macroExpression, string output)
        {
            var result = macroTransformer.ExpandMacros(macroExpression);
            if (macroExpression == "$(now.AddDays(1))" | macroExpression == "$(now)") // because of difference in time in seconds hence this
            {
                result.Substring(0, result.IndexOf("")).Should().Be(output.Substring(0, output.IndexOf("")));
                return;
            }

            result.Should().Be(output);
        }

        [Test]
        [TestCaseSource(nameof(unavailableMacroValues))]
        public void WhenMacroValueIsUnavailable_ThenSameValueIsReturned(string macroExpression, string output)
        {
            var result = macroTransformer.ExpandMacros(macroExpression);

            result.Should().Be(output);
        }
    }
}
