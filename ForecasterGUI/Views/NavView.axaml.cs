using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ForecasterGUI.Views
{
    public class NavView : UserControl
    {
        public NavView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}