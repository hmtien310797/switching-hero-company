using System;

namespace Immortal_Switch.Scripts.Helper
{
    public static class DateTimeHelper
    {
        public static bool IsNewDay(DateTime date)
        {
            var diff = DateTime.UtcNow.Date.Subtract(date.Date);
            return diff.Days > 0;
        }
    }
}