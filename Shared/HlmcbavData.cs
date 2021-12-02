using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Quandl.NET.Model.Response;

namespace Shared
{
    public record HlmcbavData(DateTime Date, double High, double Low, double? Mid, double Close, double? Bid,
        double? Ask, double Volume)
    {
        public static HlmcbavData FromObj(object[] row)
        {
            var date = DateTime.Parse((string)row[0]);
            var high = (double)row[1];
            var low = (double)row[2];
            var mid = (double?)row[3];
            var close = (double)row[4];
            var bid = (double?)row[5];
            var ask = (double?)row[6];
            var vol = (double)row[7];
            return new HlmcbavData(date, high, low, mid, close, bid, ask, vol);
        }

        public string ToCsv()
        {
            return $"{Date.ToString("yyyy-MM-dd")},{High},{Low},{Mid},{Close},{Bid},{Ask},{Volume}";
        }
        
        // Factory method to return a ohlc object from
        // a string in the following format:
        // "Date(yyyy-MM-dd),High,Low,Mid,Close,Bid,Ask,Volume"
        public static HlmcbavData FromLine(string line)
        {
            var parts = line.Split(',');
            
            return new HlmcbavData(
                DateTime.Parse(parts[0]),
                Convert.ToDouble(parts[1]),
                Convert.ToDouble(parts[2]),
                String.IsNullOrWhiteSpace(parts[3]) ? null : Convert.ToDouble(parts[3]),
                Convert.ToDouble(parts[4]),
                String.IsNullOrWhiteSpace(parts[5]) ? null : Convert.ToDouble(parts[5]),
                String.IsNullOrWhiteSpace(parts[6]) ? null : Convert.ToDouble(parts[6]),
                Convert.ToDouble(parts[7])
            );
        }

        public static List<HlmcbavData> FromFile(string filepath)
        {
            // Load existing data.
            return File.ReadAllLines(filepath).Skip(1)
                .Select(FromLine)
                .Reverse()
                .ToList();
        }
    }
}