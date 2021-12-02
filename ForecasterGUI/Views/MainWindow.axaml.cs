using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;

namespace ForecasterGUI.Views
{
    public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
    {
        private WindowNotificationManager _notificationArea;

        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif

            _notificationArea = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 2
            };


            DataContext = new MainWindowViewModel(_notificationArea);
        }


        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}