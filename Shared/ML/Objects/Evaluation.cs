using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;


namespace Shared.ML.Objects
{
    public class Evaluation
    {
        public double MeanForecastError { get; init; }
        public double MeanAbsoluteError { get; init; }
        public double MeanSquaredError { get; init; }
        public double RootMeanSquaredError { get; init; }

        public List<double> Diffs { get; init; }
        public List<TimedFeature> TestData { get; init; }
        public List<TimedFeature> TrainingData { get; private set; }
        public FeaturePrediction TestPrediction { get; init; }

        public int WindowSize { get; set; }
        public int Horizon { get; set; }
        public int SeriesLength { get; init; }

        public DateTime TrainedTillDate { get; private set; }
        public DateTime TrainedFromDate { get; set; }

        public ModelMetadata Metadata =>
            new(TrainedFromDate, TrainedTillDate, WindowSize, Horizon, SeriesLength,
                MeanForecastError, MeanAbsoluteError, MeanSquaredError);

        private MLContext _mlContext;
        private ITransformer _transformer;

        public Evaluation(MLContext mlContext, int horizon, int seriesLength, ITransformer transformer, List<TimedFeature> testPrices, List<TimedFeature> trainingPrices, FeaturePrediction p)
        {
            _mlContext = mlContext;
            _transformer = transformer;

            Horizon = horizon;
            SeriesLength = seriesLength;

            TestData = testPrices;
            TrainingData = trainingPrices;

            // Compute the difference (expected - prediction) to get the error value.
            var errors = Enumerable.Range(0, p.FeatureForecast.Length)
                .Select(i => testPrices[i].Feature - p.FeatureForecast[i])
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
            Diffs = errors;
            TestPrediction = p;

            TrainedFromDate = TrainingData.First().Date;
            TrainedTillDate = TestData.First().Date.AddDays(-1); // trained till beginning of test data.
        }

        public void PrintEvalData()
        {
            Console.WriteLine();
            Console.WriteLine("--- Performance Measures ---\n" +
                              $"Forecast Errors: {string.Join(", ", Diffs)}\n" +
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
            Console.WriteLine($"Actual Data:\n[{string.Join(", ", TestData.Select(p => p.Feature))}]");
        }

        public List<ForecastData> TrainOnTestDataAndGetForecasts()
        {
            var future_predictions = _transformer.Transform(_mlContext.Data.LoadFromEnumerable(TestData));
            var prediction =
                _mlContext.Data.CreateEnumerable<FeaturePrediction>(future_predictions, true).ToList().First();
            var forecasts = ForecastData.FromPredictionFromDate(TestPrediction, TestData.First().Date.AddDays(-1));

            var lastDate = TestData.Last().Date;
            forecasts.Concat(ForecastData.FromPredictionFromDate(prediction, lastDate));

            TrainedTillDate = TestData.Last().Date;
            TrainingData.AddRange(TestData);
            return forecasts;
        }

        public List<ForecastData> GetForecasts()
        {
            var forecasts = new List<ForecastData>();
            for (int i = 0; i < TestData.Count(); i++)
            {
                var forecast = TestPrediction.FeatureForecast[i];
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
            var modelFileName = $"{Path.GetFileName(directory)}.zip";
            var modelFilePath = Path.Combine(directory, modelFileName);

            var forecastEngine = _transformer.CreateTimeSeriesEngine<TimedFeature, FeaturePrediction>(_mlContext);
            forecastEngine.CheckPoint(_mlContext, modelFilePath);

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