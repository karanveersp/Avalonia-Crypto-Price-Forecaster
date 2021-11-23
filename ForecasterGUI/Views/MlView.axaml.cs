using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ForecasterGUI.Views
{
    public class MlView : UserControl
    {
        public MlView()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}