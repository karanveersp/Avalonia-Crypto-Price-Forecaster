using Microsoft.ML.Data;
using System;

namespace Shared.ML.Objects
{
    public class TimedFeature
    {
        [LoadColumn(0)]
        public DateTime Date;

        [LoadColumn(4)]
        public float Feature;

        public TimedFeature() { }

        public TimedFeature(DateTime date, float feature)
        {
            Date = date;
            Feature = feature;
        }
    }
}
