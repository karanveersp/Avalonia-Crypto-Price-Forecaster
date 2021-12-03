using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ForecasterGUI.Views
{
    public class PredictionResultsView : UserControl
    {
        public PredictionResultsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}