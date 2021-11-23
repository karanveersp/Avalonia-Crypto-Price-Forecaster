using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ReactiveUI;

namespace ForecasterGUI.Views
{
    public class SettingsView : ReactiveUserControl<SettingsViewModel>
    {
        public TextBox DataFileTextBox => this.FindControl<TextBox>("DataFileTextBox");
 
        public SettingsView()
        {
            InitializeComponent();

            ViewModel = RxApp.SuspensionHost.GetAppState<SettingsViewModel>();
            Trace.WriteLine(ViewModel.DataFilePath);
            DataContext = ViewModel;
            this.WhenActivated(disposableRegistration =>
            {
                this.Bind(ViewModel,
                        viewModel => viewModel.DataFilePath,
                        view => view.DataFileTextBox.Text)
                    .DisposeWith(disposableRegistration);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}