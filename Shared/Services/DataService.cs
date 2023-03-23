using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using Shared.ML.Objects;
using Quandl.NET;
using Newtonsoft.Json.Linq;
using Shared.Models;

namespace Shared.Services
{
    public class DataService : IDataService
    {
        private readonly QuandlClient Client;
        private readonly IGeckoClient CgClient;
        private readonly HttpClient HttpClient;

        private const string QuandlDatabaseCode = "BITFINEX";
        private Dictionary<string, string> SymbolToCoinGeckoId;

        public DataService(string quandlApiKey)
        {
            HttpClient = new HttpClient();

            // initialize API clients
            Client = new QuandlClient(quandlApiKey);
            CgClient = new GeckoClient(HttpClient);
            // get coin data

            Console.WriteLine("Sending get request to coingecko..");
            var coins = CgClient.GetCoinsList()
                .ConfigureAwait(false).GetAwaiter().GetResult();
            SymbolToCoinGeckoId = new Dictionary<string, string>();
            foreach (var coin in coins)
            {
                if (SymbolToCoinGeckoId.ContainsKey(coin.Symbol.ToUpper()))
                {
                    continue;
                }
                SymbolToCoinGeckoId.Add(coin.Symbol.ToUpper(), coin.Id);
            }
        }

        public List<HlmcbavData> DataAfterDate(string symbol, DateTime date)
        {
            var data = Client.Timeseries.GetDataAsync(QuandlDatabaseCode, symbol, startDate: date.Date).Result;
            var rows = data.DatasetData.Data;
            rows.Reverse();  // data is returned new -> old, so reverse.
            return rows.Select(HlmcbavData.FromObj).ToList();
        }

        public List<TimedFeature> CloseDataAfterDate(string symbol, DateTime date)
        {
            var data = Client.Timeseries.GetDataAsync(QuandlDatabaseCode, symbol, startDate: date.Date).Result;
            var prices = new List<TimedFeature>();
            // System.Console.WriteLine(string.Join(", ", data.DatasetData.ColumnNames));
            var rows = data.DatasetData.Data;
            rows.Reverse();  // data is returned new -> old, so reverse.
            foreach (var row in rows)
            {
                var rowDate = DateTime.Parse((string)row[0]);
                var rowClose = (double)row[4];


                prices.Add(new TimedFeature(rowDate, Convert.ToSingle(rowClose)));
            }

            return prices;
        }

        public TimedFeature CurrentPrice(string symbol)
        {
            // remove USD from symbol.
            var symbolWithoutUsd = symbol.Replace("USD", String.Empty);
            var id = SymbolToCoinGeckoId[symbolWithoutUsd];
            Console.WriteLine(id);
            var fetchedPrice = CgClient.GetUsdPrice(
                new string[] { id },
                new string[] { "usd" }).ConfigureAwait(false).GetAwaiter().GetResult();
            return new TimedFeature(DateTime.Now, Convert.ToSingle(fetchedPrice));
        }

        public List<OhlcData> Candles(string symbol, DateTime from)
        {
            HttpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
            var result = HttpClient.GetStringAsync($"https://api.gemini.com/v2/candles/{symbol.ToLower()}/1day").Result;
            var jsonArray = JArray.Parse(result);
            List<OhlcData> candles = new List<OhlcData>();
            foreach (JArray subArray in jsonArray.Children<JArray>())
            {
                var csv = String.Join(',', subArray.Take(5)
                    .Select(v => v.ToObject<string>()));
                var ohlcPoint = OhlcData.FromLine(csv, true);
                candles.Add(ohlcPoint);
            }

            candles.Reverse(); // ascending order instead of descending.
            return candles;
        }


    }
}