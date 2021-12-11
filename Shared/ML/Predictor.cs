using System.IO;
using System;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using Shared.ML.Objects;
using Shared.ML.Base;

namespace Shared.ML
{
    public class Predictor : BaseML
    {
        public string Symbol { get; init; }
        public string ModelFilePath { get; init; }
        public ModelMetadata Metadata { get; init; }

        public Predictor(string symbol, string modelPath, ModelMetadata modelMetadata)
        {
            Symbol = symbol;
            ModelFilePath = modelPath;
            Metadata = modelMetadata;
        }

        public PredictionData Predict(List<TimedFeature> newData)
        {
            // does not update any files, or write a new model. Uses the model it was constructed with
            // to make predictions based on the given new data

            var mlModel = LoadModelIntoMemory(ModelFilePath);
            var predictionEngine = mlModel.CreateTimeSeriesEngine<TimedFeature, FeaturePrediction>(MlContext);

            DateTime trainedToDate;

            List<ForecastData> forecast;
            
            if (newData.Any())
            {
                var predictions = newData.Select(d => (d.Date, predictionEngine.Predict(d)));
                var predictionTuples = predictions.ToList();
                trainedToDate = newData.Last().Date;
                forecast = ForecastData.FromPredictionFromDate(predictionTuples.Last().Item2, trainedToDate);
            }
            else
            {
                // No new data was provided. Metadata trained date is the last date.
                trainedToDate = Metadata.TrainedToDate;
                var prediction = predictionEngine.Predict();
                forecast = ForecastData.FromPredictionFromDate(prediction, trainedToDate);
            }
            return new PredictionData(Metadata, trainedToDate, forecast);
        }

        private ITransformer LoadModelIntoMemory(String modelPath)
        {
            ITransformer mlModel;

            using (var stream = new FileStream(modelPath,
                        FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                mlModel = MlContext.Model.Load(stream, out _);
            }

            if (mlModel == null)
            {
                throw new Exception("Failed to load model");
            }
            return mlModel;
        }


    }
}