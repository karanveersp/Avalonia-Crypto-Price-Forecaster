using System;
using System.Collections.Generic;
using CryptoForecaster.ML.Objects;

namespace CryptoForecaster.Helpers
{
    public interface IDataService
    {
        List<Price> DataAfterDate(string symbol, DateTime date); 
        Price CurrentPrice(string symbol);
    }
}