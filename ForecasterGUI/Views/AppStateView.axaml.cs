using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Mixins;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ReactiveUI;
using Splat;

namespace ForecasterGUI.Views
{
    public class AppStateView : ReactiveUserControl<AppStateViewModel>
    {
        public TextBox AppDataTextBox => this.FindControl<TextBox>("AppDataPath");
 
        public AppStateView()
        {
            InitializeComponent();
            
            ViewModel = Locator.Current.GetService<AppStateViewModel>();
 
            this.WhenActivated(disposableRegistration =>
            {
                this.Bind(ViewModel,
                        viewModel => viewModel.AppDataPath,
                        view => view.AppDataTextBox.Text)
                    .DisposeWith(disposableRegistration);
            });
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}