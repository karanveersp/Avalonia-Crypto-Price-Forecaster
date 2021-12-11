using System;
using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using Shared.ML.Base;
using Shared.ML.Objects;

namespace Shared.ML
{
    public class Trainer : BaseML
    {
        public int Horizon { get; init; }
        public int SeriesLength { get; init; }
        public string Symbol { get; init; }

        public Trainer(string symbol, int horizon, int seriesLength)
        {
            Symbol = symbol;
            Horizon = horizon;
            SeriesLength = seriesLength;
        }

        public Evaluation Train(IEnumerable<TimedFeature> data)
        {
            double minError = float.MaxValue;
            Evaluation bestEval = null!;
            data = data.ToList();

            if (Horizon >= SeriesLength)
                throw new ConstraintException($"Horizon {Horizon} must be less than Series Length {SeriesLength}");

            for (int h = Horizon; h < SeriesLength; h++)
            {
                var trainingData = data.Take(data.Count() - h).ToList();
                var testData = data.TakeLast(h).ToList();
                var evalData = OptimizedSsaModel(trainingData, testData, SeriesLength, h);
                if (evalData.MeanAbsoluteError < minError)
                {
                    bestEval = evalData;
                    bestEval.Horizon = h;
                    minError = evalData.MeanAbsoluteError;
                }
            }

            return bestEval;
        }

        [SuppressMessage("ReSharper", "ArgumentsStyleOther")]
        [SuppressMessage("ReSharper", "ArgumentsStyleNamedExpression")]
        private Evaluation OptimizedSsaModel(
            List<TimedFeature> trainingData, List<TimedFeature> testData, int seriesLength, int horizon)
        {
            Evaluation bestEval = null!;
            var minError = double.MaxValue;
            var bestWindowSize = 0;
            var trainingDataView = MlContext.Data.LoadFromEnumerable(trainingData);

            for (var windowSize = 2; windowSize < seriesLength; windowSize++)
            {
                var model = MlContext.Forecasting.ForecastBySsa(
                                outputColumnName: nameof(FeaturePrediction.FeatureForecast),
                                inputColumnName: nameof(TimedFeature.Feature),
                                windowSize: windowSize,     // each buffer is analyzed through this window
                                seriesLength: seriesLength,  // splits data into buffers of this length 
                                trainSize: trainingData.Count,
                                horizon: horizon,
                                confidenceLevel: 0.95f,
                                confidenceLowerBoundColumn: nameof(FeaturePrediction.LowerBound),
                                confidenceUpperBoundColumn: nameof(FeaturePrediction.UpperBound));

                var transformer = model.Fit(trainingDataView);
                var forecastEngine = transformer.CreateTimeSeriesEngine<TimedFeature, FeaturePrediction>(MlContext);
                var prediction = forecastEngine.Predict();

                var eval = new Evaluation(MlContext, horizon, seriesLength, transformer, testData, trainingData, prediction);
                
                if (eval.MeanAbsoluteError < minError)
                {
                    // Re-assign values contributing to a lower error.
                    bestEval = eval;
                    bestWindowSize = windowSize;
                    minError = eval.MeanAbsoluteError;
                }
            }
            
            bestEval.WindowSize = bestWindowSize;
            return bestEval;
        }

        // private void PrintPrices(List<Price> prices)
        // {
        //     foreach (var p in prices)
        //     {
        //         Console.WriteLine($"{p.Date} {p.ClosingPrice:F3}");
        //     }
        // }

        // private static void Evaluate(IDataView testData, ITransformer model, MLContext mlContext)
        // {

        //     // Actual values
        //     List<float> actual =
        //         mlContext.Data.CreateEnumerable<Price>(testData, true)
        //         .Select(observed => observed.ClosingPrice).ToList();
        //     //Console.WriteLine($"Actual prices:\n[{string.Join(", ", actual)}]");


        //     // Make predictions from test data.
        //     IDataView predictions = model.Transform(testData);

        //     // Predicted values
        //     List<float> forecast =
        //         mlContext.Data.CreateEnumerable<PricePrediction>(predictions, true)
        //         .Select(prediction => prediction.PriceForecast[0]).ToList();
        //     // StockForecast[0] is the first predicted price from the historical data up to that point.
        //     //Console.WriteLine($"Forecast prices:\n[{string.Join(", ", forecast)}]");

        //     foreach (var pair in actual.Zip(forecast))
        //     {
        //         Console.WriteLine($"{pair.First:F3} {pair.Second:F3}");
        //     }

        //     // Calculate error (actual - forecast)
        //     var metrics = actual.Zip(forecast, (actualVal, forecastVal) => actualVal - forecastVal);

        //     // Get metric averages
        //     var MAE = metrics.Average(error => Math.Abs(error)); // Mean absolute error
        //     var RMSE = Math.Sqrt(metrics.Average(error => Math.Pow(error, 2))); // Root mean squared error

        //     // Output metrics
        //     Console.WriteLine("Evaluation Metrics");
        //     Console.WriteLine("---------------------");
        //     Console.WriteLine($"Mean Absolute Error: {MAE:F3}");
        //     Console.WriteLine($"Root Mean Squared Error: {RMSE:F3}\n");

        // }

    }
}