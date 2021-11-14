using System;
using System.Collections.Generic;

namespace Shared.ML.Objects
{
    public class PredictionData
    {
        public List<TimedFeature> NewDataForModel { get; init; }
        public ModelMetadata Metadata { get; init; }
        public DateTime TrainedToDate { get; init; }
        public List<ForecastData> Forecast { get; init; }

        public PredictionData(ModelMetadata metadata, DateTime trainedToDate, List<TimedFeature> newDataForModel, List<ForecastData> forecast)
        {
            Metadata = metadata;
            NewDataForModel = newDataForModel;
            Forecast = forecast;
            TrainedToDate = trainedToDate;
        }
    }
}