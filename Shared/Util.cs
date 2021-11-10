using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Shared.Services;

using Shared.ML.Objects;

namespace Shared
{
    public static class Util
    {
        public static (string, ModelMetadata) LastModifiedModelAndMetadata(string symbol, string directory)
        {
            var latestModel = Directory.GetFiles(directory)
                .Where(f => f.Contains(symbol) && f.EndsWith(".zip"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .First();
            var metadataFile = Path.Combine(directory, Path.GetFileNameWithoutExtension(latestModel) + ".json");
            var mdString = File.ReadAllText(metadataFile);
            ModelMetadata md = JsonSerializer.Deserialize<ModelMetadata>(mdString);
            return (latestModel, md);
        }

        /// Used to update the existing data file for symbol to the latest data available.
        public static void UpdateDatasetToLatest(string symbol, string dataFilePath, IDataService dataService)
        {
            var allPrices = Util.LoadPricesFromFile(dataFilePath);
            var newData = Util.GetLatestAvailableData(symbol, allPrices.Last().Date, dataService);
            if (newData.Count() > 0)
            {
                // update dataset file in place.
                Util.UpdateDataSetFile(symbol, dataFilePath, newData, Path.GetFileName(dataFilePath));
            }
        }

        public static string UpdateDataSetFile(string symbol, string datasetFilePath, List<Price> newData, string newFileName)
        {
            var lines = File.ReadAllLines(datasetFilePath);
            var header = lines.First();
            var linesWithoutHeader = lines.Skip(1).ToList();

            var lineDict = linesWithoutHeader.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            var dataAsRows = newData.Select(p => $"{p.Date:yyyy-MM-dd},,,,{p.ClosingPrice},,,").Reverse();
            var dataTuples = dataAsRows.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            // merge dicts
            dataTuples.ToList().ForEach(dict => lineDict[dict.Key] = dict.Value);

            var updatedRows = lineDict.OrderByDescending(d => d.Key).ToList().Select(d => d.Value);
            string newFilePath = Path.Combine(Path.GetDirectoryName(datasetFilePath), newFileName);

            File.WriteAllLines(newFilePath, updatedRows.Prepend(header));
            return newFilePath;
        }

        public static string LastModifiedDataSet(string symbol, string directory)
        {
            return Directory.GetFiles(directory)
                .Where(f => Path.GetFileName(f).Equals($"{symbol}.csv"))
                .OrderByDescending(f => File.GetLastWriteTime(f))
                .First();
        }

        public static void WriteForecastsToFile(string filepath, List<ForecastData> forecasts)
        {
            var header = "Date,Forecast,LowerBound,UpperBound,BoundsDifference";
            var rows = new List<string>();
            foreach (var f in forecasts)
            {
                var row = $"{f.Date:yyyy-MM-dd},{f.Forecast},{f.LowerBound},{f.UpperBound},{f.BoundsDifference}";
                rows.Add(row);
            }

            File.WriteAllLines(filepath, rows.Prepend(header));
        }


        public static string RemoveCommas(string s)
        {
            int index = s.IndexOf(',');
            if (index == -1)
            {
                return s;
            }
            return RemoveCommas(s.Remove(index, 1));
        }

        public static string RemoveDblQuotes(string s)
        {
            int index = s.IndexOf('"');
            if (index == -1)
            {
                return s;
            }
            return RemoveDblQuotes(s.Remove(index, 1));
        }


        private static Price ToPrices(string row)
        {
            var parts = row.Split(',');
            var date = DateTime.Parse(parts[0]);
            var closingPriceString = RemoveDblQuotes(RemoveCommas(parts[4]));
            return new Price(date, Convert.ToSingle(closingPriceString));
        }

        public static IEnumerable<Price> LoadPricesFromFile(string fileName)
        {
            var lines = File.ReadAllLines(fileName).Skip(1).Reverse();
            return lines.Select(r => ToPrices(r));
        }

        public static List<Price> GetLatestAvailableData(string symbol, DateTime lastDate, IDataService dataService)
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);

            if (lastDate < yesterday)
            {
                // Model is out of date! Grab the data for missing days including yesterday,
                // and transform the model with them before making predictions for today.            
                var startDate = lastDate.AddDays(1);
                var newData = dataService.DataAfterDate(symbol, startDate);
                // newData.ForEach(p => System.Console.WriteLine($"{p.Date},{p.ClosingPrice}"));
                return newData;
            }
            return new List<Price>();
        }

        public static void WritePricesToCsv(String filepath, List<Price> prices)
        {
            var lines = prices.Select(p => $"{p.Date:yyyy-MM-dd},{p.ClosingPrice}").ToArray();
            var withHeader = new string[] { "Date,Last" }.Concat(lines);
            File.WriteAllLines(filepath, withHeader);
        }
    }
}