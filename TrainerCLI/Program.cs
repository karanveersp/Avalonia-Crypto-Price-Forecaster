using CryptoForecaster.ML;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;
using System.Linq;
using System.CommandLine;
using System.CommandLine.Invocation;
using NLog;
using CryptoForecaster.Common;
using CryptoForecaster.Helpers;
using System.Text;

namespace CryptoForecaster
{
    class Program
    {
        private static Logger logger = LogManager.GetCurrentClassLogger();

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
                        getDefaultValue: () => "",
                        description: "Optional Quandl Api Key to use for fetching latest data."
                    ),
                    new Option<int>(
                        "--horizon",
                        getDefaultValue: () => 5,
                        description: "Number of time units to predict into the future. Default 5."
                    ),
                    new Option<int>(
                        "--series-length",
                        getDefaultValue: () => 30,
                        description: "Series length for SSA model. Default 30 (to represent month)."
                    ),
                    new Option<bool>(
                        "--to-latest-data",
                        getDefaultValue: () => true,
                        description: "Optional flag whether to fetch and train upto the latest data (default true)."
                    ),
                };
            train.Handler = CommandHandler.Create<string, string, int, int, bool>(
                (symbol, quandlApiKey, horizon, seriesLength, toLatestData) =>
                {
                    NLog.GlobalDiagnosticsContext.Set("symbol", symbol);
                    if (string.IsNullOrEmpty(quandlApiKey))
                    {
                        quandlApiKey = secretApiKey;
                    }
                    // Trainer logic
                    logger.Debug("Parsed Arguments:\nSymbol: {Symbol}\nHorizon: {Horizon}\nSeriesLength: {SeriesLength}",
                        symbol, horizon, seriesLength);

                    var datasetPath = Constants.DataDir + $"{symbol}.csv";

                    var trainer = new Trainer(symbol, horizon, seriesLength, datasetPath);

                    // Perform model training
                    IDataService dataService = new DataService(quandlApiKey);
                    var eval = trainer.Train(toLatestData, dataService);

                    logger.Info("Training Metrics:\n" +
                                "Best window size: {windowSize}\n" +
                                "Mean Forecast Error: {mfe}\n" +
                                "Mean Absolute Error: {mae}\n" +
                                "Mean Squared Error: {mse}",
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
            );

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
                        getDefaultValue: () => "",
                        description: "Optional Quandl Api Key to use for fetching latest data."
                    ),
                };
            predict.Handler = CommandHandler.Create<string, bool, string>((symbol, toLatestData, quandlApiKey) =>
            {
                NLog.GlobalDiagnosticsContext.Set("symbol", symbol);
                if (string.IsNullOrEmpty(quandlApiKey))
                {
                    quandlApiKey = secretApiKey;
                }
                // Predictor logic
                logger.Debug("Parsed Arguments:\nSymbol: {Symbol}", symbol);

                IDataService ds = new DataService(quandlApiKey);


                var predictor = new Predictor(symbol, Constants.ModelsDir, toLatestData, ds);
                var predictionData = predictor.Predict();

                StringBuilder s = new StringBuilder();
                foreach (var f in predictionData.Forecast)
                {
                    s.Append($"{f.Date:yyyy-MM-dd} - ${f.Forecast} +/- ${f.BoundsDifference}\n");
                }
                logger.Info($"Forecast from: {predictionData.TrainedToDate:yyyy-MM-dd}\n{s.ToString()}");
                
                Util.WriteForecastsToFile(Constants.DataDir + $"{symbol}_prediction_forecast.csv", predictionData.Forecast);

                var datasetFile = Util.LastModifiedDataSet(symbol, Constants.DataDir);

                Util.UpdateDataSet(symbol, datasetFile, predictionData.NewDataForModel, $"{symbol}_prediction_dataset.csv");
            });

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
