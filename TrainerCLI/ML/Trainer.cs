using CryptoForecaster.ML.Base;
using System.IO;
using System;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using CryptoForecaster.ML.Objects;
using System.Collections.Generic;
using CryptoForecaster.Common;
using CryptoForecaster.Helpers;

namespace CryptoForecaster.ML
{
    public class Trainer : BaseML
    {
        public string TrainingFilePath { get; init; }
        public int Horizon { get; init; }
        public int SeriesLength { get; init; }
        public string Symbol { get; init; }

        public Trainer(string symbol, int horizon, int seriesLength, string datasetPath)
        {
            Symbol = symbol;
            Horizon = horizon;
            SeriesLength = seriesLength;
            TrainingFilePath = datasetPath;
        }

        public Evaluation Train(bool toLatestData, IDataService dataService)
        {
            if (!File.Exists(TrainingFilePath))
            {
                throw new FileNotFoundException($"Failed to find training data file ({TrainingFilePath})"); 
            }

            var allPrices = Util.LoadPricesFromFile(TrainingFilePath);

            if (toLatestData)
            {
                var newData = Util.GetLatestAvailableData(Symbol, allPrices.Last().Date, dataService);
                if (newData.Count() > 0)
                {
                    // update list of prices
                    allPrices = allPrices.Concat(newData);

                    // update dataset file in place.
                    Util.UpdateDataSet(Symbol, TrainingFilePath, newData, Path.GetFileName(TrainingFilePath));
                }
            }

            var trainingData = allPrices.Take(allPrices.Count() - Horizon).ToList();
            var testData = allPrices.TakeLast(Horizon).ToList();


            var evalData = OptimizedSsaModel(trainingData, testData, SeriesLength, Horizon);

            return evalData;

            // var future_predictions = evalData.Transformer.Transform(MlContext.Data.LoadFromEnumerable(testData));
            // var prediction =
            //     MlContext.Data.CreateEnumerable<PricePrediction>(future_predictions, true).ToList().First();

            // var lastDate = testData.Last().Date;
            // var future_forecast_rows = new List<string>{"Date,Forecast,LowerBound,UpperBound"};
            // for (int i = 0; i < Horizon; i++)
            // {
            //     var newDate = lastDate.AddDays(1);
            //     var newRow = $"{newDate:yyyy-MM-dd},{prediction.PriceForecast[i]},{prediction.LowerBound[i]},"+
            //                  $"{prediction.UpperBound[i]}";
            //     future_forecast_rows.Add(newRow);
            //     lastDate = newDate;
            // }
            // File.WriteAllLines(Constants.DataDir + $"{Symbol}_future_forecast.csv", future_forecast_rows);

            // var forecastEngine = evalData.Transformer.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);
            // forecastEngine.CheckPoint(MlContext, ModelFilePath);

            // evalData.WriteToFile($"{Symbol}_evaluation.csv");
            
            // Console.WriteLine($"Wrote model to {ModelFilePath}");
        }

        private Evaluation OptimizedSsaModel(List<Price> trainingData, List<Price> testData, int seriesLength, int horizon)
        {

            Evaluation bestEval = null;
            double minError = double.MaxValue;
            int bestWindowSize = 0;
            var trainingDataView = MlContext.Data.LoadFromEnumerable(trainingData);

            for (int i = 2; i < seriesLength; i++)
            {
                // TODO: Read - https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.timeseriescatalog.forecastbyssa?view=ml-dotnet
                var model = MlContext.Forecasting.ForecastBySsa(
                                outputColumnName: nameof(PricePrediction.PriceForecast),
                                inputColumnName: nameof(Price.ClosingPrice),
                                windowSize: i,     // each monthly interval is analyzed through this window
                                seriesLength: seriesLength,  // splits data into monthly intervals
                                trainSize: trainingData.Count(),
                                horizon: horizon,
                                confidenceLevel: 0.95f,
                                confidenceLowerBoundColumn: nameof(PricePrediction.LowerBound),
                                confidenceUpperBoundColumn: nameof(PricePrediction.UpperBound));

                // Fit the model to the training data.
                var transformer = model.Fit(trainingDataView);

                //Evaluate(MlContext.Data.LoadFromEnumerable(testData), transformer, MlContext);
                var forecastEngine = transformer.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);
                // Make a prediction using the engine.
                var prediction = forecastEngine.Predict();

                var eval = new Evaluation(MlContext, horizon, seriesLength, transformer, testData, trainingData, prediction);

                if (eval.MeanAbsoluteError < minError)
                {
                    bestEval = eval;
                    bestWindowSize = i;
                    minError = eval.MeanAbsoluteError;
                }
            }
            bestEval.WindowSize = bestWindowSize;
            return bestEval;
        }

        private void PrintPrices(List<Price> prices)
        {
            foreach (var p in prices)
            {
                Console.WriteLine($"{p.Date} {p.ClosingPrice:F3}");
            }
        }

        private static void Evaluate(IDataView testData, ITransformer model, MLContext mlContext)
        {

            // Actual values
            List<float> actual =
                mlContext.Data.CreateEnumerable<Price>(testData, true)
                .Select(observed => observed.ClosingPrice).ToList();
            //Console.WriteLine($"Actual prices:\n[{string.Join(", ", actual)}]");


            // Make predictions from test data.
            IDataView predictions = model.Transform(testData);

            // Predicted values
            List<float> forecast =
                mlContext.Data.CreateEnumerable<PricePrediction>(predictions, true)
                .Select(prediction => prediction.PriceForecast[0]).ToList();
            // StockForecast[0] is the first predicted price from the historical data up to that point.
            //Console.WriteLine($"Forecast prices:\n[{string.Join(", ", forecast)}]");

            foreach (var pair in actual.Zip(forecast))
            {
                Console.WriteLine($"{pair.First:F3} {pair.Second:F3}");                  
            }

            // Calculate error (actual - forecast)
            var metrics = actual.Zip(forecast, (actualVal, forecastVal) => actualVal - forecastVal);

            // Get metric averages
            var MAE = metrics.Average(error => Math.Abs(error)); // Mean absolute error
            var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2))); // Root mean squared error

            // Output metrics
            Console.WriteLine("Evaluation Metrics");
            Console.WriteLine("---------------------");
            Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
            Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");

        }

    }
}