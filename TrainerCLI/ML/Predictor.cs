using CryptoForecaster.ML.Base;
using System.IO;
using System;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using CryptoForecaster.ML.Objects;
using System.Linq;
using CryptoForecaster.Helpers;
using System.Collections.Generic;

namespace CryptoForecaster.ML
{
    public class Predictor : BaseML
    {
        public string Symbol { get; init; }
        public string DataDir { get; init; }
        public string ModelsDir { get; init; }
        public string ModelFilePath { get; init; }
        public ModelMetadata Metadata { get; init; }

        private bool _includeCurrentPrice;        
        private IDataService _dataService;

        public Predictor(string symbol, string modelsDir, bool includeCurrentPrice, IDataService ds)
        {
            var (modelPath, metadata) = Util.LastModifiedModelAndMetadata(symbol, modelsDir);
            Symbol = symbol;
            ModelFilePath = modelPath;
            Metadata = metadata;
            _dataService = ds;
            _includeCurrentPrice = includeCurrentPrice;
        }

        public PredictionData Predict()
        {
            // does not update any files, or write a new model. Simply reads, gets missing new data
            // after the model's metadata trainedtodate, and based on includecurrentprice, makes
            // a prediction and returns the result data object.
            
            ITransformer mlModel;


            using (var stream = new FileStream(ModelFilePath,
                FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                mlModel = MlContext.Model.Load(stream, out _);
            }

            if (mlModel == null)
            {
                throw new Exception("Failed to load model");
            }

            // Model is loaded. 
            // Check current date against model metadata.
            var today = DateTime.Now;
            var yesterday = today.Date.AddDays(-1);
            System.Console.WriteLine($"Model is trained to date: {Metadata.TrainedToDate:yyyy-MM-dd}");
            
            DateTime trainedToDate;

            var newData = Util.GetLatestAvailableData(Symbol, Metadata.TrainedToDate, _dataService);

            if (newData.Count() != 0) { 
                var dataView = MlContext.Data.LoadFromEnumerable<Price>(newData);
                mlModel.Transform(dataView);
            }

            var predictionEngine = mlModel.CreateTimeSeriesEngine<Price, PricePrediction>(MlContext);

            PricePrediction prediction;
            List<ForecastData> forecast;

            if (_includeCurrentPrice)
            {
                var currentPrice = _dataService.CurrentPrice(Symbol);
                prediction = predictionEngine.Predict(currentPrice);
                newData.Add(currentPrice);
                trainedToDate = today;
                forecast = ForecastData.FromPredictionFromDate(prediction, today);
            }
            else
            {
                trainedToDate = yesterday;
                prediction = predictionEngine.Predict();
                forecast = ForecastData.FromPredictionFromDate(prediction, yesterday);
            }

            return new PredictionData(Metadata, trainedToDate, newData, forecast);            
        }
    }
}