using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ForecasterGUI.Views
{
    public class HomeView : UserControl
    {
        public HomeView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}