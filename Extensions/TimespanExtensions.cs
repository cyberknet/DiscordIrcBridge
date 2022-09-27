using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace System
{
    public static class TimespanExtensions
    {
        public static string ToPrintableString(this TimeSpan timespan)
        {
            // always return seconds
            string format = @" s\s";

            // return minutes
            if (timespan.TotalMinutes > 0)
                format = @" m\m" + format;
            // return hours
            if (timespan.TotalHours > 1)
                format = @" h\h" + format;
            
            // return days
            if (timespan.Days > 1)
                format = @" d\d" + format;

            // return years
            if (timespan.TotalDays > 365)
                format = @" y\y" + format;

            format = format.Trim().Replace(" ", @"\ ");

            return timespan.ToString(format);
        }
    }
}
