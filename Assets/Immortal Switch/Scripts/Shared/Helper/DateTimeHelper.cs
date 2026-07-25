using System;
using System.Globalization;

namespace Immortal_Switch.Scripts.Shared.Helper
{
    public static class DateTimeHelper
    {
        public static bool TryParse(string time, out DateTime result)
        {
            const string FORMAT = "yyyy-MM-dd HH:mm:ss";

            if (!DateTime.TryParseExact(
                    time,
                    FORMAT,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// lay ra thoi gian con lai
        /// </summary>
        /// <param name="now">thoi gian hien tai can check</param>
        /// <param name="endTime">thoi gian ket thuc</param>
        /// <returns>-1 neu endtime khong dung format, tra ve tong giay con lai</returns>
        public static double CalculateRemainTime(DateTime now, string endTime)
        {
            if (!TryParse(endTime, out var result))
            {
                return -1;
            }

            return (result - now).TotalSeconds;
        }

        public static bool InTime(DateTime now, string startTime, string endTime)
        {
            if (!TryParse(startTime, out var start))
            {
                return false;
            }

            if (!TryParse(endTime, out var end))
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