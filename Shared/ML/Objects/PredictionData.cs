using System;
using System.Collections.Generic;

namespace Shared.ML.Objects
{
    public class PredictionData
    {
        public ModelMetadata Metadata { get; init; }
        public DateTime TrainedToDate { get; init; }
        public List<ForecastData> Forecast { get; init; }

        public PredictionData(ModelMetadata metadata, DateTime trainedToDate, List<ForecastData> forecast)
        {
            Metadata = metadata;
            Forecast = forecast;
            TrainedToDate = trainedToDate;
        }
    }
}