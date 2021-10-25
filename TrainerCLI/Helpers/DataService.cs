using System;
using System.Collections.Generic;
using CryptoForecaster.ML.Objects;
using Quandl.NET;
using CoinGecko.Clients;

namespace CryptoForecaster.Helpers
{
    public class DataService : IDataService
    {
        private QuandlClient Client;
        private CoinGeckoClient GeckoClient;
        private const string QuandlDatabaseCode = "BITFINEX";
        private Dictionary<string, string> SymbolToCoinGeckoId;

        public DataService(string quandlApiKey)
        {
            Client = new QuandlClient(quandlApiKey);
            GeckoClient = CoinGeckoClient.Instance;
            var coinData = GeckoClient.CoinsClient.GetAllCoinsData().Result;

            SymbolToCoinGeckoId = new Dictionary<string, string>();
            foreach (var coin in coinData)
            {
                SymbolToCoinGeckoId.Add(coin.Symbol.ToUpper(), coin.Id);
            }
        }

        public List<Price> DataAfterDate(string symbol, DateTime date)
        {
            var data = Client.Timeseries.GetDataAsync(QuandlDatabaseCode, symbol, startDate: date.Date).Result;
            var prices = new List<Price>();
            // System.Console.WriteLine(string.Join(", ", data.DatasetData.ColumnNames));
            var rows = data.DatasetData.Data;
            rows.Reverse();  // data is returned new -> old, so reverse.
            foreach (var row in rows)
            {
                var rowDate = DateTime.Parse((string)row[0]);
                var rowClose = (double)row[4];
                prices.Add(new Price(rowDate, Convert.ToSingle(rowClose)));
            }

            return prices;
        }

        public Price CurrentPrice(string symbol)
        {
            // remove USD from symbol.
            var symbolWithoutUsd = symbol.Replace("USD", String.Empty);
            var id = SymbolToCoinGeckoId[symbolWithoutUsd];
            var currentPrices = GeckoClient.SimpleClient.GetSimplePrice(
                new string[]{id},
                new string[]{"usd"}).Result;
            decimal price = currentPrices.GetValueOrDefault(id).GetValueOrDefault("usd").GetValueOrDefault();
            var currentPrice = new Price(DateTime.Now, Convert.ToSingle(price));
            return currentPrice;
        }
    }
}