using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ForecasterGUI.ViewModels
{
    [DataContract]
    public class SettingsViewModel : ViewModelBase
    {
        private string _dataFilePath;
        
        [DataMember]
        public string DataFilePath
        {
            get => _dataFilePath;
            set => this.RaiseAndSetIfChanged(ref _dataFilePath, value);
        }

        public void UpdateDataFilePath(string newPath)
        {
            Trace.WriteLine($"Updating data file path to: {newPath}");
            DataFilePath = newPath;
        }

        public SettingsViewModel()
        {
            _dataFilePath = DataFilePath = Path.Join(App.LocalAppDataDir, "BTCUSD.csv");
        }

    }
}