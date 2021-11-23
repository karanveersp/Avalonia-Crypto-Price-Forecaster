using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class HistoricalDataView : ReactiveUserControl<HistoricalDataViewModel>
    {
        public ComboBox SymbolComboBox => this.Find<ComboBox>("SymbolComboBox");
        public TextBox FetchingTextBox => this.Find<TextBox>("FetchingTextBox");
        public Button FetchDataButton => this.Find<Button>("FetchDataButton");
        
        public HistoricalDataView()
        {
            InitializeComponent();
            
            this.WhenActivated(disposableRegistration =>
            {
                this.OneWayBind(ViewModel,
                        viewModel => viewModel.Symbols,
                        view => view.SymbolComboBox.Items)
                    .DisposeWith(disposableRegistration);

                this.BindCommand(ViewModel,
                    viewModel => viewModel.FetchData,
                    view => view.FetchDataButton,
                    viewModel => viewModel.SelectedSymbol.Name)
                    .DisposeWith(disposableRegistration);

                this.Bind(ViewModel,
                        viewModel => viewModel.SelectedSymbol,
                        view => view.SymbolComboBox.SelectedItem)
                    .DisposeWith(disposableRegistration);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsFetching,
                        view => view.FetchingTextBox.IsVisible)
                    .DisposeWith(disposableRegistration);

            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}