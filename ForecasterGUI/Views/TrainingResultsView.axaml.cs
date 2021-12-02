using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.Models;
using ForecasterGUI.ViewModels;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class TrainingResultsView : ReactiveUserControl<TrainingResultsViewModel>
    {
        public ComboBox SymbolComboBox => this.Find<ComboBox>("SymbolComboBox");
        public ComboBox ExistingModelsComboBox => this.Find<ComboBox>("ExistingModelsComboBox");
        public Button ShowResultsButton => this.Find<Button>("ShowResultsButton");
        public Button RefreshButton => this.Find<Button>("RefreshButton");

        public TrainingResultsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
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
                
                // command
                this.BindCommand(ViewModel,
                        viewModel => viewModel.ShowResultsCmd,
                        view => view.ShowResultsButton,
                        viewModel => viewModel.SelectedExistingModel)
                    .DisposeWith(disposer);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.RefreshCmd,
                        view => view.RefreshButton,
                        viewModel => viewModel.SelectedSymbol)
                    .DisposeWith(disposer);
            });
        }
    }
}