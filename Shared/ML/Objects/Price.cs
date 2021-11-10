using Microsoft.ML.Data;
using System;

namespace Shared.ML.Objects
{
    public class Price
    {
        [LoadColumn(0)]
        public DateTime Date;

        [LoadColumn(4)]
        public float ClosingPrice;

        public Price() { }

        public Price(DateTime date, float price)
        {
            Date = date;
            ClosingPrice = price;
        }
    }
}
