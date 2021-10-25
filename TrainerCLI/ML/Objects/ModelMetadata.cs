using System;

namespace CryptoForecaster.ML.Objects
{
    public class ModelMetadata
    {
        public DateTime TrainedToDate { get; set; }
        public int WindowSize { get; set; }
        public int Horizon { get; set; }
        public int SeriesLength { get; set; }

        public ModelMetadata() { }

        public ModelMetadata(DateTime trainedToDate, int windowSize, int horizon, int seriesLength)
        {
            TrainedToDate = trainedToDate;
            WindowSize = windowSize;
            Horizon = horizon;
            SeriesLength = seriesLength;
        }
    }
}