using System;
using System.Collections.Generic;
using Shared.ML.Objects;

namespace Shared.Services
{
    public interface IDataService
    {
        List<HlmcbavData> DataAfterDate(string symbol, DateTime date);
        List<TimedFeature> CloseDataAfterDate(string symbol, DateTime date);
        TimedFeature CurrentPrice(string symbol);
        List<OhlcData> Candles(string symbol, DateTime from);
    }
}