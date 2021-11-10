using System;
using System.Collections.Generic;
using Shared.ML.Objects;

namespace Shared.Services
{
    public interface IDataService
    {
        List<Price> DataAfterDate(string symbol, DateTime date); 
        Price CurrentPrice(string symbol);
    }
}