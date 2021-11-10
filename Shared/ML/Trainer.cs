using System.Linq;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Collections.Generic;
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

        public Evaluation Train(IEnumerable<Price> data)
        {
            int bestHorizon;
            double minError = float.MaxValue;
            Evaluation bestEval = null;

            for (int h = Horizon; h < SeriesLength; h++)
            {
                var trainingData = data.Take(data.Count() - h).ToList();
                var testData = data.TakeLast(h).ToList();
                var evalData = OptimizedSsaModel(trainingData, testData, SeriesLength, h);
                if (evalData.MeanAbsoluteError < minError)
                {
                    bestHorizon = h;
                    bestEval = evalData;
                    bestEval.Horizon = h;
                    minError = evalData.MeanAbsoluteError;
                }
            }

            return bestEval;
        }

        private Evaluation OptimizedSsaModel(List<Price> trainingData, List<Price> testData, int seriesLength, int horizon)
        {

            Evaluation bestEval = null;
            double minError = double.MaxValue;
            int bestWindowSize = 0;
            var trainingDataView = MlContext.Data.LoadFromEnumerable(trainingData);

            for (int i = 2; i < seriesLength; i++)
            {
                // Reference: Read - 
                // https://docs.microsoft.com/en-us/dotnet/api/microsoft.ml.timeseriescatalog.forecastbyssa?view=ml-dotnet
                var model = MlContext.Forecasting.ForecastBySsa(
                                outputColumnName: nameof(PricePrediction.PriceForecast),
                                inputColumnName: nameof(Price.ClosingPrice),
                                windowSize: i,     // each interval is analyzed through this window
                                seriesLength: seriesLength,  // splits data into intervals
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