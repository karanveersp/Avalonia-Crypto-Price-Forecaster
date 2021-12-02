using System;
using System.Diagnostics;

namespace Shared
{
    public record OhlcData(DateTime Date, double Open, double High, double Low, double Close)
    {
        // Factory method to return a ohlc object from
        // a string in the following format:
        // "1637640000000,59885.21,60106.3,53760.0,55911.16"
        public static OhlcData FromLine(string line, bool hasTimestampDate)
        {
            var parts = line.Split(',');

            DateTime date;
            
            if (hasTimestampDate)
            {
                var utcDate = Util.UnixTimeToUtcDateTime(Convert.ToInt64(parts[0]));
                date = utcDate.AddDays(-1);
                Trace.WriteLine($"{date}, {utcDate}");
            }
            else
            {
                date = DateTime.Parse(parts[0]);
            }

            return new OhlcData(
                date,
                Convert.ToDouble(parts[1]),
                Convert.ToDouble(parts[2]),
                Convert.ToDouble(parts[3]),
                Convert.ToDouble(parts[4])
            );
        }
    }
}