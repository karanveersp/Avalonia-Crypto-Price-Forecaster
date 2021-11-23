using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ForecasterGUI.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Shared.ML.Objects;
using Shared.Services;
using Splat;

namespace ForecasterGUI.ViewModels
{
    public class HistoricalDataViewModel : ViewModelBase
    {
        
        public IEnumerable<Symbol> Symbols { get; }
        
        [Reactive]
        public Symbol SelectedSymbol { get; set; }

        private IDataService? _dataService;
        
        public string DataFilePath { [ObservableAsProperty] get; }

        public bool IsFetching { [ObservableAsProperty] get; }
        
        public ReactiveCommand<string, List<OhlcData>> FetchData { get; private set; }

        private IObservable<List<OhlcData>> FetchCandleDataAsync(string symbol)
        {
            return Observable.Start(() =>
            {
                var list = _dataService!.Candles(symbol, DateTime.UnixEpoch);
                var csv = list.Select(ohlc =>
                        $"{ohlc.Date.ToString("yyyy-MM-dd")},{ohlc.Open},{ohlc.High},{ohlc.Low},{ohlc.Close}")
                    .Prepend("Date,Open,High,Low,Close");
                File.WriteAllLines(DataFilePath, csv);
                Trace.WriteLine($"Wrote to: {DataFilePath}");
                return list;
            }).Delay(TimeSpan.FromSeconds(2));
        }

        public HistoricalDataViewModel(IDataService? dataService = null)
        {
            Symbols = new[] { new Symbol("BTCUSD"), new Symbol("ETHUSD") };
            SelectedSymbol = Symbols.First();
            
            _dataService = dataService ?? Locator.Current.GetService<IDataService>();
            
            FetchData = ReactiveCommand.CreateFromObservable<string, List<OhlcData>>(FetchCandleDataAsync);

            FetchData.IsExecuting
                .ToPropertyEx(this, x => x.IsFetching);
            
            FetchData.ThrownExceptions
                .Subscribe(error => this.Log().Error("Error fetching data!", error));

            
            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol =>
                {
                    Trace.WriteLine($"Symbol changed: {symbol.Name}");
                    return Path.Join(App.LocalAppDataDir, $"{symbol.Name}.csv");
                })
                .ObserveOn(RxApp.MainThreadScheduler)
                .ToPropertyEx(this, x => x.DataFilePath);
        }
    }
}