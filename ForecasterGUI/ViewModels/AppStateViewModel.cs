using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using ForecasterGUI.Models;
using ForecasterGUI.Views;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
using Shared;
using Splat;

namespace ForecasterGUI.ViewModels
{
    [DataContract]
    public class AppStateViewModel : ViewModelBase
    {

        [DataMember]
        public string AppDataPath
        {
            get => _appDataPath;
            set => this.RaiseAndSetIfChanged(ref _appDataPath, value);
        }

        [Reactive] public IEnumerable<HlmcbavData> HlmcbavInfo { get; set; }

        public async Task UpdateAppDataPath()
        {
            var dialog = new OpenFolderDialog();
            var result = await dialog.ShowAsync(_mainWindow);
            if (result != null)
            {
                Trace.WriteLine($"Updating app data path to: {result}");
                AppDataPath = result;
                _mainWindowViewModel.NotificationManager.Show(new Notification("Success", "Updated app data path",
                    NotificationType.Success));
            }
        }

        public List<MlModel> GetModels(string symbol)
        {
            var symbolSubdir = GetSymbolDir(symbol);
            if (Directory.Exists(symbolSubdir))
                return Directory.GetDirectories(symbolSubdir)
                    .Where(d => d.Contains(symbol, StringComparison.InvariantCultureIgnoreCase))
                    .Select(path => new MlModel(path))
                    .ToList();
            Directory.CreateDirectory(symbolSubdir);
            return new List<MlModel>();
        }

        public MlModel DefaultNewModel(string symbol)
        {
            var symbolSubdir = GetSymbolDir(symbol);
            var modelDirName = Util.ModelDirNameWithTimestamp(symbol);
            var path = Path.Join(symbolSubdir, modelDirName);
            return new MlModel(path);
        }

        public void CreateModelDirIfNotExists(string symbol, string modelName)
        {
            var symbolSubdir = GetSymbolDir(symbol);
            if (!Directory.Exists(symbolSubdir))
                Directory.CreateDirectory(symbolSubdir);
            var modelSubdir = Path.Join(symbolSubdir, modelName);
            if (!Directory.Exists(modelSubdir))
                Directory.CreateDirectory(modelSubdir);
        }
        
        public string GetSymbolDir(string symbol) => Path.Join(AppDataPath, symbol.ToUpper());  

        private string _appDataPath;
        private MainWindow _mainWindow;
        private MainWindowViewModel _mainWindowViewModel;
        
        public AppStateViewModel()
        {
            HlmcbavInfo = new List<HlmcbavData>();
            _appDataPath = AppDataPath = Path.Join(App.LocalAppDataDir);
            _mainWindow = Locator.Current.GetService<MainWindow>()!;
            _mainWindowViewModel = _mainWindow.ViewModel!;
            
        }
    }
}