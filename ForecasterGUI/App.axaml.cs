using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ForecasterGUI.Views;
using ReactiveUI;
using dotenv.net;
using Shared;
using Shared.Services;
using Splat;

namespace ForecasterGUI
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }
        
        public static string LocalAppDataDir => 
            Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "ForecasterGUI");

        public override void OnFrameworkInitializationCompleted()
        {

            Trace.WriteLine(LocalAppDataDir);
            Directory.CreateDirectory(LocalAppDataDir);

            var envVars = DotEnv.Fluent()
                .WithExceptions()
                .WithEnvFiles()
                .Read();

            var apiKey = envVars["API_KEY"];

            var dataService = new DataService(apiKey);
            Locator.CurrentMutable.RegisterConstant(dataService, typeof(IDataService));
            
            // Create the AutoSuspendHelper.
            var suspension = new AutoSuspendHelper(ApplicationLifetime);
            RxApp.SuspensionHost.CreateNewAppState = () => new SettingsViewModel();
            string stateFile = Path.Join(LocalAppDataDir, "appstate.json");
            RxApp.SuspensionHost.SetupDefaultSuspendResume(new NewtonsoftJsonSuspensionDriver(stateFile));
            new MainWindow {DataContext = new MainWindowViewModel()}.Show();
            base.OnFrameworkInitializationCompleted();
            suspension.OnFrameworkInitializationCompleted();
            base.OnFrameworkInitializationCompleted();
        }
    }
}