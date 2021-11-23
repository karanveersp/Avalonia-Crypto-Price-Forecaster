using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ForecasterGUI.ViewModels
{
    public class NavViewModel : ViewModelBase
    {
        [Reactive] 
        public ViewModelBase Center { get; set; }

        public void ToHistoricalData()
        {
            Center = new HistoricalDataViewModel();
        }

        public void ToSettings()
        {
            Center = new SettingsViewModel();
        }

        public void ToML()
        {
            Center = new MlViewModel();
        }
        
        public NavViewModel()
        {
            Center = new HomeViewModel();
        }
    }
}