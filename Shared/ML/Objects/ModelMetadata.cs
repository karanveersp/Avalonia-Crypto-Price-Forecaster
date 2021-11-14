using System;
using System.Collections.Generic;

namespace Shared.ML.Objects
{
    public class ModelMetadata
    {
        public DateTime TrainedToDate { get; set; }
        public DateTime TrainedFromDate { get; set; }
        public int WindowSize { get; set; }
        public int Horizon { get; set; }
        public int SeriesLength { get; set; }

        public ModelMetadata() { }

        public ModelMetadata(DateTime trainedFromDate, DateTime trainedToDate, int windowSize, int horizon, int seriesLength)
        {
            TrainedFromDate = trainedFromDate;
            TrainedToDate = trainedToDate;
            WindowSize = windowSize;
            Horizon = horizon;
            SeriesLength = seriesLength;
        }
    }
}