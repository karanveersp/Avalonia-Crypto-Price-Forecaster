using System.Diagnostics;
using System.IO;
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
    public class TrainerView : ReactiveUserControl<TrainerViewModel>
    {
        public CheckBox NewModelCheckBox => this.Find<CheckBox>("NewModelCheckBox");
        public ComboBox SymbolComboBox => this.Find<ComboBox>("SymbolComboBox");
        public StackPanel NewModelPanel => this.Find<StackPanel>("NewModelPanel");
        public StackPanel ExistingModelsPanel => this.Find<StackPanel>("ExistingModelsPanel");
        public TextBox NewModelTextBox => this.Find<TextBox>("NewModelTextBox");
        public ComboBox ExistingModelsComboBox => this.Find<ComboBox>("ExistingModelsComboBox");
        
        public NumericUpDown HorizonControl => this.Find<NumericUpDown>("HorizonControl");
        public NumericUpDown SeriesLengthControl => this.Find<NumericUpDown>("SeriesLengthControl");
        public Button TrainButton => this.Find<Button>("TrainButton");
        public Button GenerateButton => this.Find<Button>("GenerateButton");

        public TextBlock TrainingTextBlock => this.Find<TextBlock>("TrainingTextBlock");

        public TrainerView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
            
            this.WhenActivated(disposer =>
            {
                ViewModel.MinHorizon = 7;
                ViewModel.MaxSeriesLength = 30;

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsTraining,
                        view => view.TrainingTextBlock.IsVisible)
                    .DisposeWith(disposer);
                
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
                        viewModel => viewModel.IsNewModelContext,
                        view => view.NewModelCheckBox.IsChecked)
                    .DisposeWith(disposer);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsNewModelContext,
                        view => view.NewModelPanel.IsVisible)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.NewModel,
                        view => view.NewModelTextBox.Text,
                        mlModel => mlModel == null ? "" : mlModel.Name,
                        name =>
                        {
                            if (!string.IsNullOrEmpty(name))
                            {
                                Trace.WriteLine($"Updating Model Name to: {name}");
                                return new MlModel(Path.Join(ViewModel!.SymbolDir, name));
                            }

                            return null;
                        })
                    .DisposeWith(disposer);

                this.OneWayBind(ViewModel,
                        viewModel => viewModel.IsNewModelContext,
                        view => view.ExistingModelsPanel.IsVisible,
                        isNewModelContext => !isNewModelContext)
                    .DisposeWith(disposer);
                
                // training parameters
                this.Bind(ViewModel,
                        viewModel => viewModel.MinHorizon,
                        view => view.HorizonControl.Value)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.MaxSeriesLength,
                        view => view.SeriesLengthControl.Value)
                    .DisposeWith(disposer);

                // commands
                this.BindCommand(ViewModel,
                        viewModel => viewModel.TrainTheModel,
                        view => view.TrainButton)
                    .DisposeWith(disposer);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.GenerateNameCmd,
                        view => view.GenerateButton,
                        viewModel => viewModel.SelectedSymbol)
                    .DisposeWith(disposer);
            });
        }
    }
}