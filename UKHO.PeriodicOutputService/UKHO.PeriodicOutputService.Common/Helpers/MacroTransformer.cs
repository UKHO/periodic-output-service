using System.Globalization;
using System.Text.RegularExpressions;
using UKHO.PeriodicOutputService.Common.Providers;
using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.Helpers
{
    public class MacroTransformer : IMacroTransformer
    {
        private readonly ICurrentDateTimeProvider currentDateTimeProvider;
        int dayOffset = 0;

        public MacroTransformer(ICurrentDateTimeProvider currentDateTimeProvider)
        {
            this.currentDateTimeProvider = currentDateTimeProvider;
        }

        public string ExpandMacros(string value)
        {
            int offsetCapture(Match match)
            {
                string capturedNumber = match.Groups[1].Value;
                return int.Parse(capturedNumber.Replace(" ", ""));
            }

            string now_Year(Match match)
                => currentDateTimeProvider.CurrentDateTime.Year.ToString();

            string nowAddDays_Year(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Year.ToString();
            };

            string now_WeekNumber(Match match)
                => WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Week.ToString();

            string now_WeekNumberYear(Match match)
                => WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime).Year.ToString();

            string now_WeekNumberPlusWeeks(Match match)
            {
                dayOffset = offsetCapture(match);
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Week.ToString();
            }

            string now_WeekNumberPlusWeeksYear(Match match)
            {
                dayOffset = offsetCapture(match);
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset * 7)).Year.ToString();
            };

            string nowAddDays_WeekNumber(Match match)
            {
                dayOffset = offsetCapture(match);
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Week.ToString();
            };

            string nowAddDays_WeekYear(Match match)
            {
                dayOffset = offsetCapture(match);
                return WeekNumber.GetUKHOWeekFromDateTime(currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset)).Year.ToString();
            };

            string nowAddDays_Date(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).ToString(CultureInfo.InvariantCulture);
            };

            string now_Date(Match match)
                => currentDateTimeProvider.CurrentDateTime.ToString(CultureInfo.InvariantCulture);

            string now_Day(Match match)
                => currentDateTimeProvider.CurrentDateTime.Day.ToString();

            string nowAddDays_Day(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Day.ToString();
            };

            string now_Month(Match match)
                => currentDateTimeProvider.CurrentDateTime.Month.ToString();

            string nowAddDays_Month(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).Month.ToString();
            };

            string now_MonthName(Match match)
                => currentDateTimeProvider.CurrentDateTime.ToString("MMMM");

            string nowAddDays_MonthName(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).ToString("MMMM");
            };

            string now_MonthShortName(Match match)
                => currentDateTimeProvider.CurrentDateTime.ToString("MMM");

            string nowAddDays_MonthShortName(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).ToString("MMM");
            };

            string now_DayName(Match match)
                => currentDateTimeProvider.CurrentDateTime.ToString("dddd");

            string nowAddDays_DayName(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).ToString("dddd");
            };

            string now_DayShortName(Match match)
                => currentDateTimeProvider.CurrentDateTime.ToString("ddd");

            string nowAddDays_DayShortName(Match match)
            {
                dayOffset = offsetCapture(match);
                return currentDateTimeProvider.CurrentDateTime.AddDays(dayOffset).ToString("ddd");
            };

            var replacementExpressions = new Dictionary<string, Func<Match, string>>
            {
                {@"\$\(\s*now\.Year\s*\)", now_Year},
                {@"\$\(\s*now\.Year2\s*\)", (match) => now_Year(match).Substring(2,2) },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year\s*\)", nowAddDays_Year},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Year2\s*\)", (match) => nowAddDays_Year(match).Substring(2,2)},

                {@"\$\(\s*now\.Month\s*\)", now_Month },
                {@"\$\(\s*now\.Month2\s*\)", (match) => now_Month(match).PadLeft(2, '0') },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Month\s*\)", nowAddDays_Month},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Month2\s*\)", (match) => nowAddDays_Month(match).PadLeft(2, '0')},

                {@"\$\(\s*now\.Day\s*\)", now_Day },
                {@"\$\(\s*now\.Day2\s*\)", (match) => now_Day(match).PadLeft(2, '0') },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Day\s*\)", nowAddDays_Day},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).Day2\s*\)", (match) => nowAddDays_Day(match).PadLeft(2, '0')},

                {@"\$\(\s*now\.MonthName\s*\)", now_MonthName },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).MonthName\s*\)", nowAddDays_MonthName},

                {@"\$\(\s*now\.MonthShortName\s*\)", now_MonthShortName },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).MonthShortName\s*\)", nowAddDays_MonthShortName},

                {@"\$\(\s*now\.DayName\s*\)", now_DayName },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).DayName\s*\)", nowAddDays_DayName},

                {@"\$\(\s*now\.DayShortName\s*\)", now_DayShortName },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).DayShortName\s*\)", nowAddDays_DayShortName},

                {@"\$\(\s*now\.WeekNumber\s*\)",now_WeekNumber },
                {@"\$\(\s*now\.WeekNumber2\s*\)",(match) => now_WeekNumber(match).PadLeft(2,'0')},
                {@"\$\(\s*now\.WeekNumber\.Year\s*\)", now_WeekNumberYear },
                {@"\$\(\s*now\.WeekNumber\.Year2\s*\)", (match) => now_WeekNumberYear(match).Substring(2,2) },
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\)", now_WeekNumberPlusWeeks },
                {@"\$\(\s*now\.WeekNumber2\s*([+-]\s*\d+)\)",(match) => now_WeekNumberPlusWeeks(match).PadLeft(2,'0')},
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year\)", now_WeekNumberPlusWeeksYear},
                {@"\$\(\s*now\.WeekNumber\s*([+-]\s*\d+)\.Year2\)", (match) => now_WeekNumberPlusWeeksYear(match).Substring(2,2) },

                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\s*\)",nowAddDays_WeekNumber },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber2\s*\)",(match) => nowAddDays_WeekNumber(match).PadLeft(2,'0')},
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year\s*\)", nowAddDays_WeekYear },
                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\).WeekNumber\.Year2\s*\)", (match) => nowAddDays_WeekYear(match).Substring(2,2)},

                {@"\$\(\s*now.AddDays\(\s*([+-]?\s*\d+)\s*\)\s*\)", nowAddDays_Date },
                {@"\$\(\s*now\s*\)", now_Date},
            };

            if (string.IsNullOrEmpty(value))
                return value;

            return replacementExpressions.Aggregate(value,
                (input, kv) =>
                {
                    var match = Regex.Match(input, kv.Key, RegexOptions.IgnoreCase);
                    while (match.Success)
                    {
                        int end = Math.Min(match.Index + match.Length, input.Length);
                        input = input[..match.Index] +
                                match.Result(kv.Value(match)) +
                                input[end..];

                        match = Regex.Match(input, kv.Key, RegexOptions.IgnoreCase);
                    }

                    return input;
                });
        }
    }
}
