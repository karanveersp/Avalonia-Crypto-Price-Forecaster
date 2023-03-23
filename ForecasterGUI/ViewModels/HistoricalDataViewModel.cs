using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using ForecasterGUI.Models;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Shared.Services;
using Splat;

namespace ForecasterGUI.ViewModels
{
    public class HistoricalDataViewModel : ViewModelBase
    {
        [Reactive]
        public Symbol SelectedSymbol { get; set; }
        public IEnumerable<Symbol> Symbols { get; }

        private ObservableAsPropertyHelper<IEnumerable<HlmcbavData>> hlmcbavPoints;
        public IEnumerable<HlmcbavData> HlmcbavPoints => hlmcbavPoints.Value;

        // Data file path OAPH
        public string DataFilePath { [ObservableAsProperty] get; }

        // Fetching OAPH
        public bool IsFetching { [ObservableAsProperty] get; }

        // Display graphs OAPH
        private ObservableAsPropertyHelper<bool> displayGraphTabs;
        public bool DisplayGraphTabs => displayGraphTabs.Value;


        // Command
        public ReactiveCommand<string, List<HlmcbavData>> FetchData { get; private set; }

        // Charts view model ref
        [Reactive]
        public ViewModelBase HistoricalChartsViewModel { get; private set; }

        // Private fields
        private IDataService? _dataService;
        private IObservable<List<HlmcbavData>> FetchDataAsync(string symbol)
        {
            return Observable.Start(() => Util.FetchOverwriteExistingData(symbol, _dataService, DataFilePath));
        }
        private AppStateViewModel _appStateViewModel;

        // Constructor
        public HistoricalDataViewModel(IDataService? dataService = default)
        {
            _appStateViewModel = Locator.Current.GetService<AppStateViewModel>()!;

            Symbols = App.SupportedCurrencies.Select(s => new Symbol(s)).ToList();
            SelectedSymbol = Symbols.First();

            _dataService = dataService ?? Locator.Current.GetService<IDataService>();

            FetchData = ReactiveCommand.CreateFromObservable<string, List<HlmcbavData>>(FetchDataAsync);

            hlmcbavPoints = FetchData.ToProperty(this, x => x.HlmcbavPoints);

            displayGraphTabs = this.WhenAnyValue(x => x.HlmcbavPoints)
                .WhereNotNull()
                .Do(points =>
                {
                    _appStateViewModel.HlmcbavInfo = points;
                    HistoricalChartsViewModel = new HistoricalChartsViewModel();
                })
                .Select(points => points.Any())
                .ToProperty(this, x => x.DisplayGraphTabs);

            FetchData.IsExecuting
                .ToPropertyEx(this, x => x.IsFetching);

            FetchData.ThrownExceptions
                .Subscribe(error => this.Log().Error("Error fetching data!", error));


            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol =>
                {
                    Trace.WriteLine($"Symbol changed: {symbol.Name}");
                    return Path.Join(_appStateViewModel.AppDataPath, $"{symbol.Name}.csv");
                })
                .ToPropertyEx(this, x => x.DataFilePath);
        }
    }
}