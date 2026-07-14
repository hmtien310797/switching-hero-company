using System;
using System.Globalization;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public static class DateTimeHelper
    {
        public static bool InTime(DateTime now, string startTime, string endTime)
        {
            const string FORMAT = "yyyy-MM-dd HH:mm:ss";

            if (!DateTime.TryParseExact(
                    startTime,
                    FORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var start))
            {
                return false;
            }

            if (!DateTime.TryParseExact(
                    endTime,
                    FORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var end))
            {
                return false;
            }

            return now >= start && now <= end;
        }

        public static bool IsNewDay(DateTime date)
        {
            var diff = DateTime.UtcNow.Date.Subtract(date.Date);
            return diff.Days > 0;
        }
    }
}