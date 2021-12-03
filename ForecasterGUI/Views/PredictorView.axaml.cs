using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class PredictorView : ReactiveUserControl<PredictorViewModel>
    {
        public TextBox ModelsDirTextBox => this.Find<TextBox>("ModelsDirTextBox");

        public ComboBox SymbolComboBox => this.Find<ComboBox>("SymbolComboBox");
        public ComboBox ExistingModelsComboBox => this.Find<ComboBox>("ExistingModelsComboBox");

        public CheckBox IncludeDataForMissingDates => this.Find<CheckBox>("IncludeDataForMissingDates");
        public CheckBox IncludeCurrentPrice => this.Find<CheckBox>("IncludeCurrentPrice");
        public CheckBox IncludeCustomPrice => this.Find<CheckBox>("IncludeCustomPrice");

        public TextBox CustomPriceTextBox => this.Find<TextBox>("CustomPriceTextBox");
        
        public Button ForecastButton => this.Find<Button>("ForecastButton");
        public TextBlock ForecastingTextBlock => this.Find<TextBlock>("ForecastingTextBlock");
        public Button RefreshButton => this.Find<Button>("RefreshButton");

        public PredictorView()
        {
            InitializeComponent();

            this.WhenActivated(disposer =>
            {
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Symbols,
                        view => view.SymbolComboBox.Items)
                    .DisposeWith(disposer);
                
                this.Bind(ViewModel,
                        viewModel => viewModel.SelectedSymbol,
                        view => view.SymbolComboBox.SelectedItem)
                    .DisposeWith(disposer); 

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.ExistingModels,
                        view => view.ExistingModelsComboBox.Items)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.SelectedExistingModel,
                        view => view.ExistingModelsComboBox.SelectedItem)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                    viewModel => viewModel.IncludeDataForMissingDates,
                    view => view.IncludeDataForMissingDates.IsChecked)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.IncludeCurrentPrice,
                        view => view.IncludeCurrentPrice.IsChecked)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.IncludeCustomPrice,
                        view => view.IncludeCustomPrice.IsChecked)
                    .DisposeWith(disposer);
                
                this.Bind(ViewModel,
                        viewModel => viewModel.CustomPrice,
                        view => view.CustomPriceTextBox.Text)
                    .DisposeWith(disposer);
                
                

                // Disable CustomPriceTextBox if IncludeCurrentPrice = true
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.IncludeCurrentPrice,
                    view => view.CustomPriceTextBox.IsEnabled,
                    incCurrPrice => !incCurrPrice
                ).DisposeWith(disposer);
                
                // Disable IncludeCustomPrice if IncludeCurrentPrice = true
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.IncludeCurrentPrice,
                    view => view.IncludeCustomPrice.IsEnabled,
                    incCurrPrice => !incCurrPrice
                ).DisposeWith(disposer);
                
                // Disable IncludeCurrentPrice if IncludeCustomPrice = true
                this.OneWayBind(ViewModel,
                    viewModel => viewModel.IncludeCustomPrice,
                    view => view.IncludeCurrentPrice.IsEnabled,
                    incCustomPrice => !incCustomPrice
                ).DisposeWith(disposer);
                
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.ModelsDirectory,
                        view => view.ModelsDirTextBox.Text)
                    .DisposeWith(disposer);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsForecasting,
                        view => view.ForecastingTextBlock.IsVisible)
                    .DisposeWith(disposer);
                
                // command
                this.BindCommand(ViewModel,
                        viewModel => viewModel.PredictCmd,
                        view => view.ForecastButton)
                    .DisposeWith(disposer);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.RefreshCmd,
                        view => view.RefreshButton,
                        viewModel => viewModel.SelectedSymbol)
                    .DisposeWith(disposer);
            });

        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}