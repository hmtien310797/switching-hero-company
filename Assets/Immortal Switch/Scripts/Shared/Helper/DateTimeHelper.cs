using System;
using System.Globalization;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public static class DateTimeHelper
    {
        /// <summary>
        /// lay ra thoi gian con lai
        /// </summary>
        /// <param name="now">thoi gian hien tai can check</param>
        /// <param name="endTime">thoi gian ket thuc</param>
        /// <returns>-1 neu endtime khong dung format, tra ve tong giay con lai</returns>
        public static double CalculateRemainTime(DateTime now, string endTime)
        {
            const string FORMAT = "yyyy-MM-dd HH:mm:ss";

            if (!DateTime.TryParseExact(
                    endTime,
                    FORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var end))
            {
                return -1;
            }

            return (end - now).TotalSeconds;
        }

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

        public static TimeSpan GetRemainingTimeToday()
        {
            var now = DateTime.Now;
            var tomorrow = now.Date.AddDays(1);
            return tomorrow - now;
        }

        public static bool IsNewDay(DateTime date)
        {
            var diff = DateTime.UtcNow.Date.Subtract(date.Date);
            return diff.Days > 0;
        }
    }
}