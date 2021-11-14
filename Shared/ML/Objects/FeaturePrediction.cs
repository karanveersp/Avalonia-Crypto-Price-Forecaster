using System;

namespace Shared.ML.Objects
{
    public class FeaturePrediction
    {
        public float[] FeatureForecast { get; set; }
        public float[] LowerBound { get; set; }
        public float[] UpperBound { get; set; }

        public void PrintForecastValuesAndIntervals()
        {
            Console.WriteLine($"Forecasted values:");
            Console.WriteLine("[{0}]", string.Join(", ", FeatureForecast));
            Console.WriteLine($"Confidence intervals:");
            for (int index = 0; index < FeatureForecast.Length; index++)
                Console.Write($"[{LowerBound[index]} -" +
                    $" {UpperBound[index]}] ");
            Console.WriteLine();
        }

    }
}
