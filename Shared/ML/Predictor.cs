using System.IO;
using System;
using Microsoft.ML;
using Microsoft.ML.Transforms.TimeSeries;
using System.Linq;
using System.Collections.Generic;

using Shared.Services;
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

            DateTime trainedToDate;

            if (newData.Count() > 0)
            {
                // some new data was provided (possibly including today's current price or hypothetical price)
                // Train the model on the data, and then make the prediction.
                var dataView = MlContext.Data.LoadFromEnumerable<TimedFeature>(newData);
                mlModel.Transform(dataView);
                trainedToDate = newData.Last().Date;
            }
            else
            {
                // No new data was provided. Metadata trained date is the last date.
                trainedToDate = Metadata.TrainedToDate;
            }

            var predictionEngine = mlModel.CreateTimeSeriesEngine<TimedFeature, FeaturePrediction>(MlContext);
            var prediction = predictionEngine.Predict();
            var forecast = ForecastData.FromPredictionFromDate(prediction, trainedToDate);

            return new PredictionData(Metadata, trainedToDate, newData, forecast);
        }

        private ITransformer LoadModelIntoMemory(String modelPath)
        {
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
            return mlModel;
        }


    }
}