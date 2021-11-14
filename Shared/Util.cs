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

        public static float CalculatePercentChange(float previous, float current)
        {
            if (previous == 0)
                return 0;

            if (current == 0)
                return -100;


            var change = ((current - previous) / previous) * 100;
            return change;
        }

        public static IEnumerable<float> ToPercentChanges(IEnumerable<float> values)
        {
            return Enumerable
                .Range(1, values.Count())
                .Select(i => CalculatePercentChange(values.ElementAt(i - 1), values.ElementAt(i)));
        }

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
            var allPrices = Util.LoadFeatureFromFile(dataFilePath, DateTime.UnixEpoch, ClosingPriceParser);
            var newData = Util.GetLatestAvailableData(symbol, allPrices.Last().Date, dataService);
            if (newData.Count() > 0)
            {
                // update dataset file in place.
                Util.UpdateDataSetFile(symbol, dataFilePath, newData, Path.GetFileName(dataFilePath));
            }
        }

        public static string UpdateDataSetFile(string symbol, string datasetFilePath, List<TimedFeature> newData, string newFileName)
        {
            var lines = File.ReadAllLines(datasetFilePath);
            var header = lines.First();
            var linesWithoutHeader = lines.Skip(1);

            var lineDict = linesWithoutHeader.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            var dataAsRows = newData.Select(p => $"{p.Date:yyyy-MM-dd},,,,{p.Feature},,,").Reverse();
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


        public static TimedFeature ClosingPriceParser(string row)
        {
            var parts = row.Split(',');
            var date = DateTime.Parse(parts[0]);
            var closingPriceString = RemoveDblQuotes(RemoveCommas(parts[4]));
            return new TimedFeature(date, Convert.ToSingle(closingPriceString));
        }

        public static IEnumerable<TimedFeature> LoadFeatureFromFile(string fileName, DateTime startDate, Func<string, TimedFeature> featureParser)
        {
            var lines = File.ReadAllLines(fileName).Skip(1).Reverse();
            return lines.Select(r => featureParser(r))
                .Where(p => p.Date >= startDate);
        }

        public static IEnumerable<TimedFeature> LoadPercentChangesFromFile(string fileName, DateTime startDate, Func<string, TimedFeature> featureParser)
        {
            var data = LoadFeatureFromFile(fileName, startDate, featureParser);
            var pctChanges = new List<TimedFeature>();
            for (int i = 1; i < data.Count(); i++)
            {
                pctChanges.Add(new TimedFeature(
                    data.ElementAt(i).Date,
                    CalculatePercentChange(data.ElementAt(i - 1).Feature, data.ElementAt(i).Feature))
                );
            }
            return pctChanges;
        }

        public static List<TimedFeature> GetLatestAvailableData(string symbol, DateTime lastDate, IDataService dataService)
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
            return new List<TimedFeature>();
        }

        public static void WritePricesToCsv(String filepath, List<TimedFeature> prices)
        {
            var lines = prices.Select(p => $"{p.Date:yyyy-MM-dd},{p.Feature}").ToArray();
            var withHeader = new string[] { "Date,Last" }.Concat(lines);
            File.WriteAllLines(filepath, withHeader);
        }
    }
}