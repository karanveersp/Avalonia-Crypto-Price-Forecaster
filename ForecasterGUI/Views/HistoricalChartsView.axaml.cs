using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ForecasterGUI.Views
{
    public class HistoricalChartsView : UserControl
    {
        public HistoricalChartsView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}