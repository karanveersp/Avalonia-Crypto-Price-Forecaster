using System;

namespace Shared
{
    public record OhlcData(DateTime Date, double Open, double High, double Low, double Close)
    {
        // Factory method to return a ohlc object from
        // a string in the following format:
        // "1637640000000,59885.21,60106.3,53760.0,55911.16"
        public static OhlcData FromLine(string line)
        {
            var parts = line.Split(',');
            return new OhlcData(
                Util.UnixTimeToDateTime(Convert.ToInt64(parts[0])),
                Convert.ToDouble(parts[1]),
                Convert.ToDouble(parts[2]),
                Convert.ToDouble(parts[3]),
                Convert.ToDouble(parts[4])
            );
        }
    }
}