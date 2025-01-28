using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.Models
{
    public class FormattedWeekNumber
    {
        public string Week { get; }
        public string Year { get; }
        public string YearWeek { get; }
        public string YearShort { get; }

        public FormattedWeekNumber(WeekNumber weekNumber)
        {
            Week = weekNumber.Week.ToString("00");
            Year = weekNumber.Year.ToString("0000");
            YearWeek = $"{Year} / {Week}";
            YearShort = Year.Substring(2);
        }
    }
}
