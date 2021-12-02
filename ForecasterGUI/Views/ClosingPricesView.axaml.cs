using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class ClosingPricesView : ReactiveUserControl<ClosingPricesViewModel>
    {
        public DatePicker StartDatePicker => this.Find<DatePicker>("StartDatePicker");
        public Button RefitButton => this.Find<Button>("RefitButton");
        
        public ClosingPricesView()
        {
            InitializeComponent();

            this.WhenActivated(disposer =>
            {

                StartDatePicker.SelectedDate = ViewModel!.StartDate;
                StartDatePicker.MaxYear = new DateTimeOffset(ViewModel!.LastDate);
                StartDatePicker.MinYear = new DateTimeOffset(ViewModel!.StartDate);

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