using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using Splat;
using Notification = Avalonia.Controls.Notifications.Notification;

namespace ForecasterGUI.ViewModels
{
    public class TrainingResultsViewModel : ViewModelBase
    {
        private readonly AppStateViewModel _appState;
        private MainWindowViewModel _mainWindowViewModel;

        [Reactive]
        public Symbol SelectedSymbol { get; set; }
        public IEnumerable<Symbol> Symbols { get; } 
        
        [Reactive]
        public MlModel SelectedExistingModel { get; set; }
        
        [Reactive]
        public List<MlModel> ExistingModels { get; set; }
        
        public ReactiveCommand<MlModel, Unit> ShowResultsCmd { get; }
        public ReactiveCommand<Symbol, Unit> RefreshCmd { get; }
        
        [Reactive]
        public ViewModelBase ResultsChartViewModel { get; set; }

        private void ShowResults(MlModel model)
        {
            ResultsChartViewModel = new TrainingResultsChartViewModel(model);
        }

        public void RefreshModels(Symbol symbol)
        {
            ExistingModels = _appState.GetModels(symbol.Name);
        }

        public TrainingResultsViewModel(string selectedSymbol = "")
        {
            _appState = Locator.Current.GetService<AppStateViewModel>()!;
            _mainWindowViewModel = Locator.Current.GetService<MainWindow>()!.ViewModel!;
            // populate symbols
            Symbols = App.SupportedCurrencies.Select(s => new Symbol(s)).ToList();
            
            SelectedSymbol = string.IsNullOrEmpty(selectedSymbol) 
                ? Symbols.First() 
                : Symbols.First(s => s.Name.Equals(selectedSymbol));
            
            this.WhenAnyValue(x => x.SelectedSymbol)
                .Select(symbol => _appState.GetModels(symbol.Name))
                .Subscribe(models =>
                {
                    ExistingModels = models;
                    ResultsChartViewModel = null!;
                });

            
            var canShowResults = this.WhenAnyValue(
                x => x.SelectedSymbol, 
                x => x.SelectedExistingModel,
                (symbol, existingModel) =>
                {
                    var validModel = existingModel != null && !string.IsNullOrEmpty(existingModel.DirPath);
                    return !string.IsNullOrEmpty(symbol.Name) && validModel;
                });
            
            ShowResultsCmd = ReactiveCommand.Create<MlModel>(ShowResults, canShowResults);
            ShowResultsCmd.ThrownExceptions.Subscribe(ex =>
            {
                var modelDir = SelectedExistingModel!.DirPath;
                var errorLog = Path.Join(modelDir, "errors.txt");
                Util.WriteStackTrace(errorLog, ex);
                var n = new Notification("Error",
                    ex.Message,
                    NotificationType.Error,
                    TimeSpan.FromSeconds(10));
                _mainWindowViewModel.NotificationManager.Show(n);
            });
            RefreshCmd = ReactiveCommand.Create<Symbol>(RefreshModels);
        }
    }
}