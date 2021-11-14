using System;
using System.Collections.Generic;
using Shared.ML.Objects;

namespace Shared.Services
{
    public interface IDataService
    {
        List<TimedFeature> DataAfterDate(string symbol, DateTime date);
        TimedFeature CurrentPrice(string symbol);
    }
}