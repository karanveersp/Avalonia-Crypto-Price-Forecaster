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
    public class PercentChangesView : ReactiveUserControl<PercentChangesViewModel>
    {
        public TextBox PeriodInput => this.Find<TextBox>("PeriodInput");
        public TextBox ThresholdInput => this.Find<TextBox>("ThresholdInput");
        public Button RecomputeButton => this.Find<Button>("RecomputeButton");
        
        public PercentChangesView()
        {
            InitializeComponent();

            this.WhenActivated(disposer =>
            {
                this.Bind(ViewModel,
                    viewModel => viewModel.Period,
                    view => view.PeriodInput.Text)
                    .DisposeWith(disposer);

                this.Bind(ViewModel,
                        viewModel => viewModel.Threshold,
                        view => view.ThresholdInput.Text)
                    .DisposeWith(disposer);
                
                this.BindCommand(ViewModel,
                        viewModel => viewModel.Recompute,
                        view => view.RecomputeButton)
                    .DisposeWith(disposer);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}