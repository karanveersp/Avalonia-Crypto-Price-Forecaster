using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Avalonia.Controls.Notifications;
using ForecasterGUI.Models;
using ForecasterGUI.Views;
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
    public class TrainerViewModel : ViewModelBase
    {
        [Reactive] public Symbol SelectedSymbol { get; set; }
        public IEnumerable<Symbol> Symbols { get; }

        [Reactive] public bool IsNewModelContext { get; set; }

        [Reactive] public MlModel SelectedExistingModel { get; set; }

        [Reactive] public IEnumerable<MlModel> ExistingModels { get; set; }

        [Reactive] public MlModel NewModel { get; set; }

        public string SymbolDir { [ObservableAsProperty] get; }

        public bool IsTraining { [ObservableAsProperty] get; }

        // Training Parameter Observables
        [Reactive] public int MinHorizon { get; set; }

        [Reactive] public int MaxSeriesLength { get; set; }

        public ReactiveCommand<Unit, string> TrainTheModel { get; }
        public ReactiveCommand<Symbol, string> GenerateNameCmd { get; }

        // Private fields
        private string GenerateName(Symbol symbol)
        {
            return Util.ModelDirNameWithTimestamp(symbol.Name);
        }

        private IObservable<string> TrainImplAsync()
        {
            return Observable.Start(() =>
            {
                var symbol = SelectedSymbol.Name;
                var modelDir = IsNewModelContext ? NewModel.DirPath : SelectedExistingModel.DirPath;

                string s = $"Horizon: {MinHorizon}\n" +
                           $"Series Length: {MaxSeriesLength}\n" +
                           $"IsNewModel?: {IsNewModelContext}\n" +
                           $"ModelDir: {modelDir}\n";
                Trace.WriteLine(s);

                // Create model directory
                Directory.CreateDirectory(modelDir);

                var data = FetchUpdatedData(symbol, modelDir);

                var features = data
                    .Select(d => new TimedFeature(d.Date, Convert.ToSingle(d.Close)));
                
                var trainer = new Trainer(symbol, MinHorizon, MaxSeriesLength);
                
                var eval = trainer.Train(features);

                Trace.WriteLine("Training Metrics:\n" +
                                $"Best horizon: {eval.Horizon}\n" +
                                $"Best window size: {eval.WindowSize}\n" +
                                $"Mean Forecast Error: {eval.MeanForecastError}\n" +
                                $"Mean Absolute Error: {eval.MeanAbsoluteError}\n" +
                                $"Mean Squared Error: {eval.MeanSquaredError}\n"
                );

                // Output forecasts
                var forecasts = eval.TrainOnTestDataAndGetForecasts();


                var forecastsPath = Path.Join(modelDir, $"{symbol}_training_forecast.csv");

                var testingSplitFile = Path.Combine(modelDir, $"{symbol}_testing_split.csv");
                var trainingSplitFile = Path.Combine(modelDir, $"{symbol}_training_split.csv");

                Util.WritePricesToCsv(testingSplitFile, eval.TestData);
                Util.WritePricesToCsv(trainingSplitFile, eval.TrainingData);

                Trace.WriteLine($"Wrote training split data to: {Path.GetFileName(trainingSplitFile)}");
                Trace.WriteLine($"Wrote testing split data to: {Path.GetFileName(testingSplitFile)}");

                eval.WriteForecastsToFile(forecastsPath, forecasts);
                Trace.WriteLine($"Wrote forecasts to {Path.GetFileName(forecastsPath)}");

                // Output model
                var (modelFilePath, _) = eval.WriteModelToDir(symbol, modelDir);
                Trace.WriteLine($"Wrote model to: {Path.GetFileName(modelFilePath)}");

                return Path.GetFileName(modelDir);
            });
        }

        private readonly AppStateViewModel _appState;
        private readonly IDataService _dataService;
        private readonly MainWindowViewModel _mainWindowViewModel;


        /// <summary>
        /// FetchUpdatedData will refresh the symbol.csv file in the app data folder,
        /// and copy it to the model directory.
        /// It returns a list of the Date/High/Low/Mid/Close/Bid/Ask/Volume data points. 
        /// </summary>
        /// <param name="symbol">The relevant trading symbol such as BTCUSD.</param>
        /// <param name="modelDir">The directory path for all model artifacts.</param>
        /// <returns></returns>
        private List<HlmcbavData> FetchUpdatedData(string symbol, string modelDir)
        {
            var dataFile = Path.Join(_appState.AppDataPath, $"{symbol}.csv");
            var modelDataFile = Path.Join(modelDir, Path.GetFileName(dataFile));

            var data = Util.FetchOverwriteExistingData(symbol, _dataService, dataFile);
            
            // Copy data file to model dir
            if (File.Exists(modelDataFile))
            {
                File.Delete(modelDataFile);
            }
            File.Copy(dataFile, modelDataFile);

            return data;
        }

        public TrainerViewModel(IDataService? dataService = null)
        {
            _appState = Locator.Current.GetService<AppStateViewModel>()!;
            _mainWindowViewModel = Locator.Current.GetService<MainWindow>()!.ViewModel!;

            // populate symbols
            Symbols = App.SupportedCurrencies.Select(s => new Symbol(s)).ToList();
            SelectedSymbol = Symbols.First();

            // service for future use
            _dataService = dataService ?? Locator.Current.GetService<IDataService>()!;

            // Defaults
            IsNewModelContext = true;

            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol =>
                {
                    Trace.WriteLine($"Symbol changed: {symbol.Name}");
                    return _appState.GetSymbolDir(symbol.Name);
                })
                .ToPropertyEx(this, x => x.SymbolDir);

            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol => _appState.GetModels(symbol.Name))
                .Subscribe(models => { ExistingModels = models; });

            var canTrain = this.WhenAnyValue(
                x => x.SelectedSymbol, x => x.NewModel,
                x => x.SelectedExistingModel,
                x => x.IsNewModelContext,
                (symbol, newModel, existingModel, isNewModelContext) =>
                {
                    bool validModel;
                    if (isNewModelContext)
                        validModel = newModel != null && !string.IsNullOrEmpty(newModel.DirPath);
                    else
                    {
                        validModel = existingModel != null && !string.IsNullOrEmpty(existingModel.DirPath);
                    }

                    return !string.IsNullOrEmpty(symbol.Name) && validModel;
                });
            var canGenerateName = this.WhenAnyValue(
                x => x.SelectedSymbol, x => x.IsNewModelContext,
                (symbol, isNewModelContext) => { return !string.IsNullOrEmpty(symbol.Name) && isNewModelContext; });

            TrainTheModel = ReactiveCommand.CreateFromObservable(TrainImplAsync, canTrain);
            
            TrainTheModel
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe( modelName =>
            {
                var n = new Notification("Training Successful", $"{modelName} trained!", NotificationType.Success,
                    TimeSpan.FromSeconds(10));
                _mainWindowViewModel.NotificationManager.Show(n);
            });

            // Error handler
            TrainTheModel.ThrownExceptions.Subscribe(ex =>
            {
                var modelDir = IsNewModelContext ? NewModel!.DirPath : SelectedExistingModel!.DirPath;
                var errorLog = Path.Join(modelDir, "errors.txt");
                Util.WriteStackTrace(errorLog, ex);
                var n = new Notification("Training Error",
                    ex.Message,
                    NotificationType.Error,
                    TimeSpan.FromSeconds(10));
                _mainWindowViewModel.NotificationManager.Show(n);
            });

            TrainTheModel.IsExecuting.ToPropertyEx(this, x => x.IsTraining);

            GenerateNameCmd = ReactiveCommand.Create<Symbol, string>(GenerateName, canGenerateName);

            GenerateNameCmd.Subscribe(name =>
            {
                if (string.IsNullOrEmpty(name)) return;
                Trace.WriteLine($"Updating Model Name to: {name}");
                NewModel = new MlModel(Path.Join(SymbolDir, name));
            });

            this.WhenAnyValue(x => x.IsNewModelContext)
                .Subscribe(isNewModelContext =>
                {
                    if (!isNewModelContext)
                    {
                        ExistingModels = _appState.GetModels(SelectedSymbol.Name);
                    }
                });
        }
    }
}