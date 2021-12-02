using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using Shared.Services;
using Shared.ML.Objects;

namespace Shared
{
    public static class Util
    {
        public static IEnumerable<TimedFeature> GetSimpleMovingAverage(IEnumerable<TimedFeature> source,
            int sampleLength)
        {
            var sourceFeatureList = source.ToList();
            var sma = sourceFeatureList.Select(f => Convert.ToDouble(f.Feature))
                .SimpleMovingAverage(7);
            var results = sourceFeatureList.Zip(sma, (s, a) => new TimedFeature(s.Date, Convert.ToSingle(a)));
            return results;
        }

        public static IEnumerable<TimedFeature> ToClosingPrices(IEnumerable<OhlcData> candles)
        {
            return candles.Select(ohlc => new TimedFeature(ohlc.Date, Convert.ToSingle(ohlc.Close)));
        }

        public static IEnumerable<TimedFeature> ToClosingPrices(IEnumerable<HlmcbavData> candles)
        {
            return candles.Select(hlmc => new TimedFeature(hlmc.Date, Convert.ToSingle(hlmc.Close)));
        }

        public static DateTime GetLocalDateTime(DateTime utcDateTime, TimeZoneInfo timeZone)
        {
            utcDateTime = DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);
            DateTime time = TimeZoneInfo.ConvertTime(utcDateTime, timeZone);
            return time;
        }

        public static DateTime UnixTimeToUtcDateTime(long unixTime)
        {
            var dt = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dt = dt.AddMilliseconds(unixTime);
            return dt;
        }

        public static float CalculatePercentChange(float previous, float current)
        {
            if (previous == 0)
                return 0;

            if (current == 0)
                return -100;


            var change = ((current - previous) / previous) * 100;
            return change;
        }

        public static IEnumerable<TimedFeature> ToPercentChanges(TimedFeature[] source, int nPeriods)
        {
            var pctChanges = new TimedFeature[source.Length - nPeriods];
            int j = 0;
            for (int i = nPeriods; i < source.Length; i++)
            {
                var cur = source[i];
                var prev = source[i - nPeriods];
                pctChanges[j] = new TimedFeature(
                    cur.Date,
                    CalculatePercentChange(prev.Feature, cur.Feature));
                j++;
            }

            return pctChanges;
        }

        public static string ModelDirNameWithTimestamp(string symbol)
        {
            return $"{symbol.ToUpper()}_{DateTime.Now.ToString("yyMMdd_hhmmss")}";
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

        public static void WriteStackTrace(string filePath, Exception ex)
        {
            using (var writer = new StreamWriter(filePath, true))
            {
                writer.WriteLine("-----------------------------------------------------------------------------");
                writer.WriteLine("Date : " + DateTime.Now);
                writer.WriteLine();

                while (ex != null)
                {
                    writer.WriteLine(ex.GetType().FullName);
                    writer.WriteLine("Message : " + ex.Message);
                    writer.WriteLine("StackTrace : " + ex.StackTrace);

                    ex = ex.InnerException;
                }
            }
        }

        /// <summary>
        /// LoadModelMetadata returns a metadata object by parsing the
        /// *model*.json file where model is the name of the model directory.
        /// Returns null if no metadata file exists.
        /// </summary>
        /// <param name="modelDirPath">Path to model directory containing training artifacts.</param>
        /// <returns></returns>
        public static ModelMetadata? LoadModelMetadata(string modelDirPath)
        {
            var metadataFile = Path.Combine(modelDirPath, Path.GetFileName(modelDirPath) + ".json");

            if (!File.Exists(metadataFile))
                return null;

            var mdString = File.ReadAllText(metadataFile);
            return JsonSerializer.Deserialize<ModelMetadata>(mdString);
        }

        public static (string, string, string) GetTrainingTestForecastPaths(string modelDirPath)
        {
            var symbol = modelDirPath.Split("\\")[^2];
            return (Path.Join(modelDirPath, $"{symbol}_training_split.csv"),
                Path.Join(modelDirPath, $"{symbol}_testing_split.csv"),
                Path.Join(modelDirPath, $"{symbol}_training_forecast.csv"));
        }

        /// Used to update the existing data file for symbol to the latest data available.
        public static void UpdateDatasetToLatest(string symbol, string dataFilePath, IDataService dataService)
        {
            var allPrices = LoadFeatureFromFile(dataFilePath, DateTime.UnixEpoch, ClosingPriceParser);
            var newData = GetLatestAvailableData(symbol, allPrices.Last().Date, dataService);
            if (newData.Count() > 0)
            {
                // update dataset file in place.
                UpdateDataSetFile(dataFilePath, Path.GetFileName(dataFilePath), newData);
            }
        }

        public static void UpdateDataSetFile(string datasetFilePath, string newFileName, List<TimedFeature> newData)
        {
            var lines = File.ReadAllLines(datasetFilePath);
            var header = lines.First();
            var linesWithoutHeader = lines.Skip(1);

            var lineDict = linesWithoutHeader.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            var dataAsRows = newData.Select(p => $"{p.Date.ToString("yyyy-MM-dd")},,,,{p.Feature},,,").Reverse();
            var dataTuples = dataAsRows.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            // merge dicts
            dataTuples.ToList().ForEach(dict => lineDict[dict.Key] = dict.Value);

            var updatedRows = lineDict.OrderByDescending(d => d.Key).ToList().Select(d => d.Value);
            string newFilePath = Path.Combine(Path.GetDirectoryName(datasetFilePath)!, newFileName);

            File.WriteAllLines(newFilePath, updatedRows.Prepend(header));
        }

        public static void UpdateDataSetFile(string datasetFilePath, string newFileName, List<HlmcbavData> newData)
        {
            var lines = File.ReadAllLines(datasetFilePath);
            var header = lines.First();
            var linesWithoutHeader = lines.Skip(1);

            var lineDict = linesWithoutHeader.Select(line =>
            {
                var date = DateTime.Parse(line.Split(",")[0]);
                return (date, line);
            }).ToDictionary(t => t.date, t => t.line);

            var dataAsRows = newData.Select(p => p.ToCsv()).Reverse();
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

        public static List<HlmcbavData> FetchOverwriteExistingData(string symbol, IDataService dataService,
            string dataFilePath)
        {
            if (File.Exists(dataFilePath))
            {
                // Read last date from file
                var dataFromFile = HlmcbavData.FromFile(dataFilePath);
                var lastDate = dataFromFile.Last().Date;
                // Fetch new data and overwrite the existing file.
                var newData = GetLatestAvailableData(symbol, lastDate, dataService);
                if (newData.Any())
                {
                    UpdateDataSetFile(dataFilePath, Path.GetFileName(dataFilePath), newData);
                    return HlmcbavData.FromFile(dataFilePath);
                }

                return dataFromFile; // return the originally read data.
            }

            // file doesn't exist, so write fresh data.
            return FetchAndWriteAllAvailableData(symbol, dataService, dataFilePath);
        }

        public static List<HlmcbavData> FetchAndWriteAllAvailableData(string symbol, IDataService dataService,
            string dataFilePath)
        {
            var list = dataService.DataAfterDate(symbol, DateTime.UnixEpoch);
            var csv = list.Select(d => d.ToCsv()).Reverse()
                .Prepend("Date,High,Low,Mid,Close,Bid,Ask,Volume");
            File.WriteAllLines(dataFilePath, csv);
            return list;
        }

        public static TimedFeature ClosingPriceParser(string row)
        {
            var parts = row.Split(',');
            var date = DateTime.Parse(parts[0]);
            var closingPriceString = RemoveDblQuotes(RemoveCommas(parts[4]));
            return new TimedFeature(date, Convert.ToSingle(closingPriceString));
        }

        public static IEnumerable<TimedFeature> LoadFeatureFromFile(string fileName, DateTime startDate,
            Func<string, TimedFeature> featureParser)
        {
            var lines = File.ReadAllLines(fileName).Skip(1).Reverse();
            return lines.Select(r => featureParser(r))
                .Where(p => p.Date >= startDate);
        }

        public static IEnumerable<TimedFeature> LoadPercentChangesFromFile(string fileName, DateTime startDate,
            Func<string, TimedFeature> featureParser)
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

        public static List<HlmcbavData> GetLatestAvailableData(string symbol, DateTime lastDate,
            IDataService dataService)
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);

            if (lastDate.Date < yesterday.Date)
            {
                // Model is out of date! Grab the data for missing days including yesterday,
                // and transform the model with them before making predictions for today.            
                var startDate = lastDate.AddDays(1);
                Trace.WriteLine($"Last date in file is {lastDate} so making API call!");
                var newData = dataService.DataAfterDate(symbol, startDate);
                // newData.ForEach(p => System.Console.WriteLine($"{p.Date},{p.ClosingPrice}"));
                return newData;
            }

            Trace.WriteLine($"Last date in file is {lastDate} so no API call made!");
            return new List<HlmcbavData>();
        }

        public static List<TimedFeature> GetLatestAvailableCloseData(string symbol, DateTime lastDate,
            IDataService dataService)
        {
            var today = DateTime.Now;
            var yesterday = today.AddDays(-1);

            if (lastDate < yesterday)
            {
                // Model is out of date! Grab the data for missing days including yesterday,
                // and transform the model with them before making predictions for today.            
                var startDate = lastDate.AddDays(1);
                var newData = dataService.CloseDataAfterDate(symbol, startDate);
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