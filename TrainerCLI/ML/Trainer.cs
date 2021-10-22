using CryptoForecaster.ML.Base;
using CryptoForecaster.Objects;
using System.IO;
using System;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using CryptoForecaster.ML.Objects;
using System.Collections.Generic;
using CryptoForecaster.Common;

namespace CryptoForecaster.ML
{
    public class Trainer : BaseML
    {
        public void Train(ProgramArguments args)
        {
            if (!File.Exists(args.TrainingFileName))
            {
                Console.WriteLine($"Failed to find training data file ({args.TrainingFileName})");
                return;
            }

            // Name of the training file must be of format BTCUSD/ETHUSD.csv etc.
            string currency = Path.GetFileNameWithoutExtension(args.TrainingFileName);

            int horizon = 5;
            int seriesLength = 30;

            var allPrices = LoadPricesFromFile(args.TrainingFileName);
            var trainingData = allPrices.Take(allPrices.Count() - horizon).ToList();
            var testData = allPrices.TakeLast(horizon).ToList();

            WritePricesToCsv($"{currency}_training-split.csv", trainingData);
            WritePricesToCsv($"{currency}_testing-split.csv", testData);

            var trainingDataView = MlContext.Data.LoadFromEnumerable(trainingData);

            // TODO: Read - https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.timeseriescatalog.forecastbyssa?view=ml-dotnet

            //var model = MlContext.Forecasting.ForecastBySsa(
            //    outputColumnName: nameof(PricePrediction.PriceForecast),
            //    inputColumnName: nameof(Price.ClosingPrice),
            //    windowSize: 29,     // each monthly interval is analyzed through this window
            //    seriesLength: seriesLength,  // splits data into monthly intervals
            //    trainSize: 3000,
            //    horizon: 5,
            //    confidenceLevel: 0.95f,
            //    confidenceLowerBoundColumn: nameof(PricePrediction.LowerBound),
            //    confidenceUpperBoundColumn: nameof(PricePrediction.UpperBound));

            //// Fit the model to the training data.
            //var transformer = model.Fit(trainingDataView);

            ////Evaluate(MlContext.Data.LoadFromEnumerable(testData), transformer, MlContext);
            //var evalData = EvaluateV2(transformer, testData);

            var (transformer, evalData) = OptimizedSsaModel(trainingDataView, testData, seriesLength, horizon);

            evalData.PrintEvalData();
            evalData.PrintTestData();
            evalData.PrintForecast();
            evalData.WriteToFile($"{currency}_evaluation.csv");

            transformer.Transform(MlContext.Data.LoadFromEnumerable(testData));

            var modelFilePath = Path.Combine(AppContext.BaseDirectory, args.ModelFileName);
            var forecastEngine = transformer.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);
            forecastEngine.CheckPoint(MlContext, modelFilePath);

            Console.WriteLine($"Wrote model to {modelFilePath}");
     
        }

        private (ITransformer, EvaluationData) OptimizedSsaModel(IDataView trainingDataView, List<Price> testData, int seriesLength, int horizon)
        {
            ITransformer bestModel = null;
            EvaluationData bestEvalData = null;
            double minError = double.MaxValue;
            int bestWindowSize = 0;

            for (int i = 2; i < seriesLength; i++)
            {
                var model = MlContext.Forecasting.ForecastBySsa(
                                outputColumnName: nameof(PricePrediction.PriceForecast),
                                inputColumnName: nameof(Price.ClosingPrice),
                                windowSize: i,     // each monthly interval is analyzed through this window
                                seriesLength: seriesLength,  // splits data into monthly intervals
                                trainSize: 3000,
                                horizon: horizon,
                                confidenceLevel: 0.95f,
                                confidenceLowerBoundColumn: nameof(PricePrediction.LowerBound),
                                confidenceUpperBoundColumn: nameof(PricePrediction.UpperBound));

                // Fit the model to the training data.
                var transformer = model.Fit(trainingDataView);

                //Evaluate(MlContext.Data.LoadFromEnumerable(testData), transformer, MlContext);
                var evalData = EvaluateV2(transformer, testData);
                if (evalData.MeanAbsoluteError < minError)
                {
                    bestEvalData = evalData;
                    bestModel = transformer;
                    bestWindowSize = i;
                    minError = evalData.MeanAbsoluteError;
                }
            }
            Console.WriteLine($"Best windowSize: {bestWindowSize}");
            return (bestModel, bestEvalData);

        }

        class EvaluationData
        {
            public double MeanForecastError { get; init; }
            public double MeanAbsoluteError { get; init; }
            public double MeanSquaredError { get; init; }
            public double RootMeanSquaredError { get; init; }

            public List<double> Errors { get; init; }
            public List<Price> TestData { get; init; }
            public PricePrediction Prediction { get; init; }

            public EvaluationData(List<Price> testPrices, PricePrediction p)
            {
                TestData = testPrices;

                // Compute the difference (expected - prediction) to get the error value.
                var errors = Enumerable.Range(0, p.PriceForecast.Length)
                    .Select(i => testPrices[i].ClosingPrice - p.PriceForecast[i])
                    .Select(e => Convert.ToDouble(e)).ToList();

                // The mean forecast error value other than 0 suggests a tendency of the model
                // to over forecase (negative error) or under forecast (positive error).
                MeanForecastError = errors.Average();

                // The mean absolute error takes absolute value of the errors before taking the mean.
                MeanAbsoluteError = errors.Average(e => Math.Abs(e));

                // The mean squared error squares the error values to make them positive, and also
                // has the effect of putting more weight on large errors.
                // This highlights worse performance to those models that make large wrong forecasts.
                // 0 indicates perfect, no errors.
                MeanSquaredError = errors.Average(e => Math.Pow(e, 2));
                Errors = errors;
                Prediction = p;
            }

            public void PrintEvalData()
            {
                Console.WriteLine();
                Console.WriteLine("--- Performance Measures ---\n" +
                                  $"Forecast Errors: {string.Join(", ", Errors)}\n" +
                                  $"Mean Forecast Error: {MeanForecastError}\n" +
                                  $"Mean Absolute Error: {MeanAbsoluteError}\n" +
                                  $"Mean Squared Error: {MeanSquaredError}\n");
            }

            public void PrintForecast()
            {
                Prediction.PrintForecastValuesAndIntervals();
            }

            public void PrintTestData()
            {
                Console.WriteLine($"Actual Data:\n[{string.Join(", ", TestData.Select(p => p.ClosingPrice))}]");
            }

            public void WriteToFile(string name)
            {
                var fp = Constants.DataDir + name;
                var header = "Date,Actual,Forecast,LowerBound,UpperBound";
                var rows = new List<string>();
                for (int i = 0; i < TestData.Count(); i++)
                {
                    var forecast = Prediction.PriceForecast[i];
                    var (lowerBound, upperBound) = (Prediction.LowerBound[i], Prediction.UpperBound[i]);

                    var row = $"{TestData[i].Date:yyyy-MM-dd},{TestData[i].ClosingPrice},{forecast},{lowerBound},{upperBound}";
                    rows.Add(row);
                }
                File.WriteAllLines(fp, rows.Prepend(header));
            }
        }

        private EvaluationData EvaluateV2(ITransformer transformer, List<Price> testPrices)
        {
            var forecastEngine = transformer.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);

            // Make a prediction using the engine.
            var forecast = forecastEngine.Predict();


            //Console.WriteLine($"Actual Prices:\n[{string.Join(", ", testPrices.Select(p => p.ClosingPrice))}]");

            return new EvaluationData(testPrices, forecast);
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

        private void WritePricesToCsv(String name, List<Price> prices)
        {
            var lines = prices.Select(p => $"{p.Date:yyyy-MM-dd},{p.ClosingPrice}").ToArray();
            var withHeader = new string[]{"Date,Last"}.Concat(lines);
            File.WriteAllLines(Constants.DataDir + name, withHeader);
        }

        private double Mean(IEnumerable<double> values)
        {
            return values.Sum() * (1.0 / values.Count());
        }

        private string RemoveCommas(string s)
        {
            int index = s.IndexOf(',');
            if (index == -1)
            {
                return s;
            }
            return RemoveCommas(s.Remove(index, 1));
        }

        private string RemoveDblQuotes(string s)
        {
            int index = s.IndexOf('"');
            if (index == -1)
            {
                return s;
            }
            return RemoveDblQuotes(s.Remove(index, 1));
        }


        private Price ToStockPrices(string row)
        {
            var parts = row.Split(',');
            var date = DateTime.Parse(parts[0]);
            var closingPriceString = RemoveDblQuotes(RemoveCommas(parts[4]));
            return new Price(date, Convert.ToSingle(closingPriceString));
        }

        private IEnumerable<Price> LoadPricesFromFile(string fileName)
        {
            var lines = File.ReadAllLines(fileName).Skip(1).Reverse();
            return lines.Select(r => ToStockPrices(r));
        }

    }
}