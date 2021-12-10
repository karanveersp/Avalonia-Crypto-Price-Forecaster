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
        public DatePicker StartDatePicker => this.Find<DatePicker>("StartDatePicker");
        public Button RefitButton => this.Find<Button>("RefitButton");

        public PercentChangesView()
        {
            InitializeComponent();

            this.WhenActivated(disposer =>
            {
                StartDatePicker.SelectedDate = ViewModel!.StartDate;
                StartDatePicker.MaxYear = new DateTimeOffset(ViewModel!.LastDate);
                StartDatePicker.MinYear = new DateTimeOffset(ViewModel!.StartDate);
                
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
                
                this.Bind(ViewModel,
                        viewModel => viewModel.StartDate,
                        view => view.StartDatePicker.SelectedDate,
                        dt => new DateTimeOffset(dt),
                        dto => dto!.Value.DateTime)
                    .DisposeWith(disposer);

                this.BindCommand(ViewModel,
                        viewModel => viewModel.RefitCommand,
                        view => view.RefitButton)
                    .DisposeWith(disposer);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}