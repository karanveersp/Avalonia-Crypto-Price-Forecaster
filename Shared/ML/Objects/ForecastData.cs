using System;
using System.Collections.Generic;
using System.Linq;

namespace Shared.ML.Objects
{
    public class ForecastData
    {
        public DateTime Date { get; init; }
        public float Forecast { get; init; }
        public float UpperBound { get; init; }
        public float LowerBound { get; init; }
        public double BoundsDifference { get; init; }

        public ForecastData(DateTime date, float forecast, float upperBound, float lowerBound)
        {
            Date = date;
            Forecast = forecast;
            UpperBound = upperBound;
            LowerBound = lowerBound;
            BoundsDifference = (upperBound - lowerBound) / 2.0;
        }

        public static List<ForecastData> FromPredictionFromDate(FeaturePrediction p, DateTime date)
        {
            var forecasts = new List<ForecastData>();

            for (int i = 0; i < p.FeatureForecast.Count(); i++)
            {
                var newDate = date.AddDays(1);
                var forecastItem = new ForecastData(newDate, p.FeatureForecast[i],
                                                    p.UpperBound[i], p.LowerBound[i]);
                forecasts.Add(forecastItem);
                date = newDate;
            }
            return forecasts;
        }
    }
}