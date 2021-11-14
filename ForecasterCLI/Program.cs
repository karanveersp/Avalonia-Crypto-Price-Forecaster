
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using NLog;
using System.Text;

using Shared.ML;
using Shared.Services;
using Shared;
using System.Collections.Generic;
using Shared.ML.Objects;

namespace ForecasterCLI
{
    public static class Constants
    {
        public static readonly string DataDir = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, @"..\..\..\Data\"));
        public static readonly string RootDir = Path.GetFullPath(Path.Combine(DataDir, @"..\"));
        public static readonly string ModelsDir = Path.Combine(RootDir, @"Models\");
    }

    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

        private static void Predict(String symbol, bool toLatestData, string quandlApiKey, float customPrice)
        {
            Console.WriteLine(quandlApiKey);
            NLog.GlobalDiagnosticsContext.Set("symbol", symbol);

            // Predictor logic
            logger.Debug("Parsed Arguments:\nSymbol: {Symbol}", symbol);

            var (modelPath, metadata) = Util.LastModifiedModelAndMetadata(symbol, Constants.ModelsDir);

            // Check current date against model metadata.
            var today = DateTime.Now;
            var yesterday = today.Date.AddDays(-1);

            System.Console.WriteLine($"Model is trained from date: {metadata.TrainedFromDate:yyyy-MM-dd}");
            System.Console.WriteLine($"Model is trained to date: {metadata.TrainedToDate:yyyy-MM-dd}");

            IDataService ds = new DataService(quandlApiKey);
            List<TimedFeature> newData = new List<TimedFeature>();

            if (toLatestData)
            {
                newData = Util.GetLatestAvailableData(symbol, metadata.TrainedToDate, ds);
                var currentPrice = ds.CurrentPrice(symbol);
                newData.Add(currentPrice);
            }

            if (customPrice != float.MinValue)
            {
                // take all but last, and replace the last value with the custom feature
                newData = newData
                    .TakeWhile(t => t.Date < today)
                    .Append(new TimedFeature(today, customPrice))
                    .ToList();
            }

            var predictor = new Predictor(symbol, modelPath, metadata);
            var predictionData = predictor.Predict(newData);

            StringBuilder s = new StringBuilder();
            foreach (var f in predictionData.Forecast)
            {
                s.Append($"{f.Date:yyyy-MM-dd} - {f.Forecast} +/- {f.BoundsDifference}\n");
            }
            logger.Info($"Forecast from: {predictionData.TrainedToDate:yyyy-MM-dd}\n{s.ToString()}");

            Util.WriteForecastsToFile(Constants.DataDir + $"{symbol}_prediction_forecast.csv", predictionData.Forecast);

            var datasetFile = Constants.DataDir + $"{symbol}.csv";
            var predictionDatasetFile = Constants.DataDir + $"{symbol}_prediction_dataset.csv";
            File.Copy(datasetFile, predictionDatasetFile, overwrite: true);

            Util.UpdateDataSetFile(symbol, predictionDatasetFile, predictionData.NewDataForModel, predictionDatasetFile);
        }

        private static void Train(String symbol, String quandlApiKey, int horizon, int seriesLength,
                                  bool toLatestData, DateTime startDate, bool percentChange)
        {
            NLog.GlobalDiagnosticsContext.Set("symbol", symbol);

            // Trainer logic
            logger.Debug("Parsed Arguments:\nSymbol: {Symbol}\nHorizon: {Horizon}\nSeriesLength: {SeriesLength}",
                symbol, horizon, seriesLength);



            var datasetPath = Constants.DataDir + $"{symbol}.csv";
            IDataService dataService = new DataService(quandlApiKey);
            if (toLatestData)
            {
                Util.UpdateDatasetToLatest(symbol, datasetPath, dataService);
            }

            IEnumerable<TimedFeature> data;

            // Prepare dataset
            if (!percentChange)
            {

                data = Util.LoadFeatureFromFile(datasetPath, startDate, Util.ClosingPriceParser);
            }
            else
            {
                data = Util.LoadPercentChangesFromFile(datasetPath, startDate, Util.ClosingPriceParser);
            }

            var trainer = new Trainer(symbol, horizon, seriesLength);
            var eval = trainer.Train(data);

            logger.Info("Training Metrics:\n" +
                        "Best horizon size: {bestHorizon}\n" +
                        "Best window size: {windowSize}\n" +
                        "Mean Forecast Error: {mfe}\n" +
                        "Mean Absolute Error: {mae}\n" +
                        "Mean Squared Error: {mse}",
                        eval.Horizon,
                        eval.WindowSize, eval.MeanForecastError, eval.MeanAbsoluteError, eval.MeanSquaredError);

            // Output forecasts
            var forecasts = eval.TrainOnTestDataAndGetForecasts();


            var forecastsPath = Constants.DataDir + $"{symbol}_training_forecast.csv";

            var testingSplitFile = Path.Combine(Constants.DataDir, $"{symbol}_testing_split.csv");
            var trainingSplitFile = Path.Combine(Constants.DataDir, $"{symbol}_training_split.csv");

            Util.WritePricesToCsv(testingSplitFile, eval.TestData);
            Util.WritePricesToCsv(trainingSplitFile, eval.TrainingData);

            logger.Info($"Wrote training split data to: {Path.GetFileName(trainingSplitFile)}");
            logger.Info($"Wrote testing split data to: {Path.GetFileName(testingSplitFile)}");

            eval.WriteForecastsToFile(forecastsPath, forecasts);
            logger.Info($"Wrote forecasts to {Path.GetFileName(forecastsPath)}");

            // Output model
            var (modelFilePath, _) = eval.WriteModelToDir(symbol, Constants.ModelsDir);
            logger.Info($"Wrote model to: {Path.GetFileName(modelFilePath)}");

        }


        static int Main(string[] args)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddUserSecrets<Program>()
                .Build();
            var secretProvider = config.Providers.First();
            if (!secretProvider.TryGet("QuandlApiKey", out var apiKey))
            {
                System.Console.WriteLine("Could not load QuandlApiKey from secrets.");
            }
            apiKey = String.IsNullOrEmpty(apiKey) ? "" : apiKey;
            return ParseAndExecute(args, apiKey);
        }

        static int ParseAndExecute(string[] args, string secretApiKey = "")
        {
            var train = new Command("train", "Train a model")
                {
                    new Argument<string>(
                        "--symbol",
                        description: "Specify symbol target. Used to name the model, and fetch data. Ex BTCUSD."
                    ),
                    new Option<string>(
                        "--quandl-api-key",
                        getDefaultValue: () => secretApiKey,
                        description: "Optional Quandl Api Key to use for fetching latest data."
                    ),
                    new Option<int>(
                        "--horizon",
                        getDefaultValue: () => 4,
                        description: "Number of time units to predict into the future."
                    ),
                    new Option<int>(
                        "--series-length",
                        getDefaultValue: () => 30,
                        description: "Series length for SSA model."
                    ),
                    new Option<bool>(
                        "--to-latest-data",
                        getDefaultValue: () => true,
                        description: "Optional flag whether to fetch and train upto the latest data."
                    ),
                    new Option<DateTime>(
                        "--start-date",
                        getDefaultValue: () => DateTime.UnixEpoch,
                        description: "Optional start date for dataset training. Defaults to oldest date available."
                    ),
                    new Option<bool>(
                        "--percentChange",
                        getDefaultValue: () => false,
                        description: "Optional flag whether to model based on percent changes instead of price."
                    )
                };
            train.Handler = CommandHandler
                .Create<string, string, int, int, bool, DateTime, bool>(Train);

            var predict = new Command("predict", "Make a prediction from a trained model")
                {
                   new Argument<string>(
                        "--symbol",
                        description: "Specify symbol target. Ex BTCUSD or XMRUSD."
                    ),
                    new Option<bool>(
                        "--to-latest-data",
                        getDefaultValue: () => true,
                        description: "Optional flag for whether to train till current price before predicting."
                    ),
                    new Option<string>(
                        "--quandl-api-key",
                        getDefaultValue: () => secretApiKey,
                        description: "Optional Quandl Api Key to use for fetching latest data."
                    ),
                    new Option<float>(
                        "--custom-price",
                        getDefaultValue: () => float.MinValue,
                        description: "Optional price to pass as the last price before forecasting."
                    ),
                };
            predict.Handler = CommandHandler.Create<string, bool, string, float>(Predict);

            // Root command with some options
            var rootCommand = new RootCommand()
            {
                train,
                predict
            };
            rootCommand.Description = "CLI app to train the model or make a prediction.";

            return rootCommand.InvokeAsync(args).Result;
        }
    }
}
