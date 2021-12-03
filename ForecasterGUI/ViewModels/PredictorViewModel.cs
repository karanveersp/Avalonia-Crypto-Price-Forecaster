using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Controls.Notifications;
using DynamicData;
using ForecasterGUI.Models;
using ForecasterGUI.Views;
using Microsoft.VisualBasic;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Shared.ML;
using Shared.ML.Objects;
using Shared.Services;
using Splat;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace ForecasterGUI.ViewModels
{
    public class PredictorViewModel : ViewModelBase
    {

        private readonly MainWindowViewModel _mainWindowViewModel;
        private readonly AppStateViewModel _appState;
        private readonly IDataService _dataService;

        // Symbol selection
        [Reactive]
        public Symbol SelectedSymbol { get; set; }
        public IEnumerable<Symbol> Symbols { get; } 
        
        // Model selection
        [Reactive]
        public MlModel SelectedExistingModel { get; set; }
        [Reactive]
        public List<MlModel> ExistingModels { get; set; }
        
        public string ModelsDirectory { [ObservableAsProperty] get; }
        public bool IsForecasting { [ObservableAsProperty] get; }
        
        // Prediction parameters
        [Reactive]
        public bool IncludeCurrentPrice { get; set; }
        
        [Reactive]
        public bool IncludeCustomPrice { get; set; }
        
        [Reactive]
        public bool IncludeDataForMissingDates { get; set; }
        
        [Reactive]
        public double CustomPrice { get; set; }
        
        
        // Commands
        public ReactiveCommand<Unit, Unit> PredictCmd { get; }
        public ReactiveCommand<Symbol, Unit> RefreshCmd { get; }
        
        [Reactive]
        public ViewModelBase PredictionResultsViewModel { get; set; }

        private IObservable<Unit> Predict()
        {
            return Observable.Start(() =>
            {
                var model = SelectedExistingModel;
                if (model.Metadata == null)
                    throw new NullReferenceException(
                        $"{model.Name} metadata file not found. Please retrain the model.");

                var modelPath = Path.Join(model.DirPath, $"{Path.GetFileName(model.DirPath)}.zip");
                var metadata = model.Metadata;
                var symbol = SelectedSymbol.Name;

                var today = DateTime.Today.Date;

                Trace.WriteLine($"Model is trained from date: {metadata.TrainedFromDate:yyyy-MM-dd}");
                Trace.WriteLine($"Model is trained to date: {metadata.TrainedToDate:yyyy-MM-dd}");

                List<TimedFeature> newData = new();
                List<HlmcbavData> hlmcbavData = new();

                if (IncludeDataForMissingDates)
                {
                    hlmcbavData = Util.GetLatestAvailableData(symbol, metadata.TrainedToDate, _dataService).ToList();
                    newData = hlmcbavData
                        .Select(d => new TimedFeature(d.Date, Convert.ToSingle(d.Close))).ToList();
                }

                if (IncludeCurrentPrice && IncludeCustomPrice)
                {
                    throw new Exception("Cannot include both current price and custom price in prediction.");
                }

                if (IncludeCurrentPrice && !IncludeCustomPrice)
                {
                    var currentPrice = _dataService.CurrentPrice(symbol);
                    newData.Add(currentPrice);
                    hlmcbavData.Add(new HlmcbavData(currentPrice.Date, 0, 0, 0,
                        Convert.ToDouble(currentPrice.Feature), 0, 0, 0));
                }
                else if (!IncludeCurrentPrice && IncludeCustomPrice)
                {
                    newData.Add(new TimedFeature(today, Convert.ToSingle(CustomPrice)));
                    hlmcbavData.Add(new HlmcbavData(today, 0, 0, 0,
                        Convert.ToDouble(CustomPrice), 0, 0, 0));
                }

                var predictor = new Predictor(symbol, modelPath, metadata);
                var predictionData = predictor.Predict(newData);

                StringBuilder s = new();
                foreach (var f in predictionData.Forecast)
                {
                    s.Append($"{f.Date:yyyy-MM-dd} - {f.Forecast} +/- {f.BoundsDifference}\n");
                }

                Trace.WriteLine($"Forecast from: {predictionData.TrainedToDate:yyyy-MM-dd}\n{s}");

                Util.WriteForecastsToFile(Path.Join(model.DirPath, $"{symbol}_prediction_forecast.csv"),
                    predictionData.Forecast);

                var datasetFile = Path.Join(model.DirPath, $"{symbol}.csv");
                var predictionDatasetFile = Path.Join(model.DirPath, $"{symbol}_prediction_dataset.csv");
                File.Copy(datasetFile, predictionDatasetFile, overwrite: true);

                Util.UpdateDataSetFile(predictionDatasetFile, predictionDatasetFile, hlmcbavData);

                PredictionResultsViewModel = new PredictionResultsViewModel(predictionData, predictionDatasetFile);
            });
        }

        public void RefreshModels(Symbol symbol)
        {
            ExistingModels = _appState.GetModels(symbol.Name);
        }
        
        public PredictorViewModel()
        {
            _mainWindowViewModel = Locator.Current.GetService<MainWindow>()!.ViewModel!;
            _appState = Locator.Current.GetService<AppStateViewModel>()!;
            _dataService = Locator.Current.GetService<IDataService>()!;
            
            // populate symbols
            Symbols = App.SupportedCurrencies.Select(s => new Symbol(s)).ToList();

            SelectedSymbol = Symbols.First();
            ModelsDirectory = _appState.GetSymbolDir(SelectedSymbol.Name);
            
            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol => _appState.GetModels(symbol.Name))
                .Subscribe(models =>
                {
                    ExistingModels = models;
                    PredictionResultsViewModel = null!;
                });

            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(s => _appState.GetSymbolDir(s.Name))
                .ToPropertyEx(this, x => x.ModelsDirectory);

            var canPredict = this.WhenAnyValue(
                x => x.SelectedSymbol, 
                x => x.SelectedExistingModel,
                (symbol, existingModel) =>
                {
                    var validModel = existingModel != null && !string.IsNullOrEmpty(existingModel.DirPath);
                    return !string.IsNullOrEmpty(symbol.Name) && validModel;
                });
                
            PredictCmd = ReactiveCommand.CreateFromObservable(Predict, canPredict);
            PredictCmd.IsExecuting
                .ToPropertyEx(this, x => x.IsForecasting);
            
            PredictCmd.ThrownExceptions.Subscribe(ex =>
            {
                var modelDir = SelectedExistingModel!.DirPath;
                var errorLog = Path.Join(modelDir, "errors.txt");
                Util.WriteStackTrace(errorLog, ex);
                var n = new Notification("Forecast Error",
                    ex.Message,
                    NotificationType.Error,
                    TimeSpan.FromSeconds(10));
                _mainWindowViewModel.NotificationManager.Show(n);
            });
            RefreshCmd = ReactiveCommand.Create<Symbol>(RefreshModels);
        }
    }
}