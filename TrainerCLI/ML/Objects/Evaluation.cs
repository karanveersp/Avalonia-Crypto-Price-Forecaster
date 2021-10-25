using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using CryptoForecaster.ML.Objects;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;


namespace CryptoForecaster.ML
{
    public class Evaluation
        {
            public double MeanForecastError { get; init; }
            public double MeanAbsoluteError { get; init; }
            public double MeanSquaredError { get; init; }
            public double RootMeanSquaredError { get; init; }
            public ITransformer Transformer { get; init; }

            public List<double> Errors { get; init; }
            public List<Price> TestData { get; init; }
            public List<Price> TrainingData { get; private set; }
            public PricePrediction TestPrediction { get; init; }
            
            public int WindowSize { get; set; }
            public int Horizon { get; init; }
            public int SeriesLength { get; init; }

            public DateTime TrainedTillDate { get; private set; }

            private MLContext MlContext;

            public ModelMetadata Metadata {
                get {
                    return new ModelMetadata(TrainedTillDate, WindowSize, Horizon, SeriesLength);
                }
            }

            public Evaluation(MLContext mlContext, int horizon, int seriesLength, ITransformer transformer, List<Price> testPrices, List<Price> trainingPrices, PricePrediction p)
            { 
                MlContext = mlContext;
                Horizon = horizon;
                SeriesLength = seriesLength;

                Transformer = transformer;
                TestData = testPrices;
                TrainingData = trainingPrices;

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
                TestPrediction = p;
                TrainedTillDate = TestData.First().Date.AddDays(-1); // trained till beginning of test data.
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

            public void PrintTestForecast()
            {
                TestPrediction.PrintForecastValuesAndIntervals();
            }

            public void PrintTestData()
            {
                Console.WriteLine($"Actual Data:\n[{string.Join(", ", TestData.Select(p => p.ClosingPrice))}]");
            }

            public List<ForecastData> TrainOnTestDataAndGetForecasts()
            {
                var future_predictions = Transformer.Transform(MlContext.Data.LoadFromEnumerable(TestData));
                var prediction =
                    MlContext.Data.CreateEnumerable<PricePrediction>(future_predictions, true).ToList().First();
                var forecasts = ForecastData.FromPredictionFromDate(TestPrediction, TestData.First().Date.AddDays(-1));
                // for (int i = 0; i < TestData.Count(); i++)
                // {
                //     var forecast = TestPrediction.PriceForecast[i];
                //     var (lowerBound, upperBound) = (TestPrediction.LowerBound[i], TestPrediction.UpperBound[i]);
                //     var forecastItem = new ForecastData(TestData[i].Date, forecast, upperBound, lowerBound);
                //     forecasts.Add(forecastItem);
                // }
                var lastDate = TestData.Last().Date;
                forecasts.Concat(ForecastData.FromPredictionFromDate(prediction, lastDate));
                // for (int i = 0; i < prediction.PriceForecast.Count(); i++)
                // {
                //     var newDate = lastDate.AddDays(1);
                //     var forecastItem = new ForecastData(newDate, prediction.PriceForecast[i],
                //                                         prediction.UpperBound[i], prediction.LowerBound[i]);
                //     forecasts.Add(forecastItem);
                //     lastDate = newDate;
                // }
                TrainedTillDate = TestData.Last().Date;
                TrainingData.AddRange(TestData);
                return forecasts;
            }

            public List<ForecastData> GetForecasts()
            {
                var forecasts = new List<ForecastData>();
                for (int i = 0; i < TestData.Count(); i++)
                {
                    var forecast = TestPrediction.PriceForecast[i];
                    var (lowerBound, upperBound) = (TestPrediction.LowerBound[i], TestPrediction.UpperBound[i]);
                    var forecastItem = new ForecastData(TestData[i].Date, forecast, upperBound, lowerBound);
                    forecasts.Add(forecastItem);
                }
                return forecasts;
            }

            // <summary>
            // Writes the model to the given directory, adding the last date of training to the name.
            // </summary>
            // Returns a tuple of model file path, metadata file path.
            public (string, string) WriteModelToDir(string symbol, string directory)
            {
                var modelFileName = $"{symbol}_predictor_{TrainedTillDate:yyyyMMdd}.zip";
                var modelFilePath = Path.Combine(directory, modelFileName);

                var forecastEngine = Transformer.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);
                forecastEngine.CheckPoint(MlContext, modelFilePath);

                // Write the model's metadata to the same directory. 
                var metadataFileName = Path.GetFileNameWithoutExtension(modelFileName) + ".json";
                var metadataFilePath = Path.Combine(directory, metadataFileName);

                string json = JsonSerializer.Serialize(Metadata);
                File.WriteAllText(metadataFilePath, json);
                return (modelFilePath, metadataFilePath);
            }

            public void WriteForecastsToFile(string filepath, List<ForecastData> forecasts)
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


        }

}