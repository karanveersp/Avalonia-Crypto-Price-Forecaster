using System;

namespace CryptoForecaster.ML.Objects
{
    public class PricePrediction
    {
        public float[] PriceForecast { get; set; }
        public float[] LowerBound { get; set; }
        public float[] UpperBound { get; set; }

        public void PrintForecastValuesAndIntervals()
        {
            Console.WriteLine($"Forecasted values:");
            Console.WriteLine("[{0}]", string.Join(", ", PriceForecast));
            Console.WriteLine($"Confidence intervals:");
            for (int index = 0; index < PriceForecast.Length; index++)
                Console.Write($"[{LowerBound[index]} -" +
                    $" {UpperBound[index]}] ");
            Console.WriteLine();
        }

    }
}
