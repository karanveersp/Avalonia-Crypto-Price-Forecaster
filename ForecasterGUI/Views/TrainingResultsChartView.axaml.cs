using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class TrainingResultsChartView : ReactiveUserControl<TrainingResultsChartViewModel>
    {
        public TextBlock HorizonText => this.Find<TextBlock>("HorizonText");
        public TextBlock SeriesLengthText => this.Find<TextBlock>("SeriesLengthText");
        public TextBlock WindowSizeText => this.Find<TextBlock>("WindowSizeText");
        public TextBlock TrainStartDateText => this.Find<TextBlock>("TrainStartDateText");
        public TextBlock TrainEndDateText => this.Find<TextBlock>("TrainEndDateText");
 
        public TextBlock MeanForecastErrorText => this.Find<TextBlock>("MeanForecastErrorText");
        public TextBlock MeanAbsoluteErrorText => this.Find<TextBlock>("MeanAbsoluteErrorText");
        public TextBlock MeanSquaredErrorText => this.Find<TextBlock>("MeanSquaredErrorText");

        public TrainingResultsChartView()
        {
            InitializeComponent();

            this.WhenActivated(disposer =>
            {
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Horizon,
                        view => view.HorizonText.Text)
                    .DisposeWith(disposer);
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.SeriesLength,
                        view => view.SeriesLengthText.Text)
                    .DisposeWith(disposer); 
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.WindowSize,
                        view => view.WindowSizeText.Text)
                    .DisposeWith(disposer); 
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.TrainedFromDate,
                        view => view.TrainStartDateText.Text,
                        date => date.ToString("yyyy-MM-dd"))
                    .DisposeWith(disposer);
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.TrainedToDate,
                        view => view.TrainEndDateText.Text,
                        date => date.ToString("yyyy-MM-dd"))
                    .DisposeWith(disposer);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.MeanForecastError,
                        view => view.MeanForecastErrorText.Text,
                        value => $"{value:F2}")
                    .DisposeWith(disposer);
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.MeanAbsoluteError,
                        view => view.MeanAbsoluteErrorText.Text,
                        value => $"{value:F2}")
                    .DisposeWith(disposer);


                this.OneWayBind(ViewModel,
                        viewModel => viewModel.MeanSquaredError,
                        view => view.MeanSquaredErrorText.Text,
                        value => $"{value:F2}")
                    .DisposeWith(disposer);

            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}