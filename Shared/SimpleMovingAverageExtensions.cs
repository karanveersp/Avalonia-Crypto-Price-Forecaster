using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shared
{
    public static class SimpleMovingAverageExtensions
    {
        public static IEnumerable<double> SimpleMovingAverage(
            this IEnumerable<double> source, int sampleLength)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (sampleLength <= 0) throw new ArgumentException("Invalid sample length");

            return SimpleMovingAverageImpl(source, sampleLength);
        }

        private static IEnumerable<double> SimpleMovingAverageImpl(
            IEnumerable<double> source, int sampleLength)
        {
            Queue<double> sample = new Queue<double>(sampleLength);
            foreach (double d in source)
            {
                if (sample.Count == sampleLength)
                {
                    sample.Dequeue();
                }
                sample.Enqueue(d);
                yield return sample.Average();
            }
        }
    }
}