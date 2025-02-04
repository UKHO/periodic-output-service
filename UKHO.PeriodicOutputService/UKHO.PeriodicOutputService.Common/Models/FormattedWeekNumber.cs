using UKHO.WeekNumberUtils;

namespace UKHO.PeriodicOutputService.Common.Models
{
    public class FormattedWeekNumber
    {
        /// <summary>
        /// Week number formatted as WW.
        /// </summary>
        public string Week { get; }

        /// <summary>
        /// Year formatted as YYYY.
        /// </summary>
        public string Year { get; }

        /// <summary>
        /// Year and week number formatted as YYYY / WW.
        /// </summary>
        public string YearWeek { get; }

        /// <summary>
        /// Year formatted as YY.
        /// </summary>
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
