using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;
using ForecasterGUI.ViewModels;
using ForecasterGUI.Views;
using ReactiveUI;
using dotenv.net;
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

        public static string[] SupportedCurrencies = null!;

        public override void OnFrameworkInitializationCompleted()
        {

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                Directory.CreateDirectory(LocalAppDataDir);

                // Parse dotenv file and initialize instance of data service.
                var envVars = DotEnv.Fluent()
                    .WithEnvFiles()
                    .WithEnvFiles()
                    .Read();
                var apiKey = envVars.ContainsKey("API_KEY") 
                    ? envVars["API_KEY"] 
                    : "";
                var dataService = new DataService(apiKey);

                SupportedCurrencies = envVars.ContainsKey("SUPPORTED_CURRENCIES")
                    ? envVars["SUPPORTED_CURRENCIES"].Split(',')
                    : new[] { "BTCUSD", "ETHUSD", "XMRUSD" };
                
                // Singleton data service registration with DI framework.
                Locator.CurrentMutable.RegisterConstant(dataService, typeof(IDataService));
                
                var mainWindow = new MainWindow();
                
                // Register MainWindow with DI to allow other components to access the main view model
                // for window notification command creation.
                Locator.CurrentMutable.RegisterConstant(mainWindow, typeof(MainWindow));

                // Create the AutoSuspendHelper for the application config.
                var suspension = new AutoSuspendHelper(ApplicationLifetime);
                RxApp.SuspensionHost.CreateNewAppState = () => new AppStateViewModel();
                string stateFile = Path.Join(LocalAppDataDir, "appstate.json");
                RxApp.SuspensionHost.SetupDefaultSuspendResume(new NewtonsoftJsonSuspensionDriver(stateFile));
                suspension.OnFrameworkInitializationCompleted();
                
                Locator.CurrentMutable.RegisterConstant(
                    RxApp.SuspensionHost.GetAppState<AppStateViewModel>(), typeof(AppStateViewModel));
                
                // Display the main view.
                desktop.MainWindow = mainWindow;
               
                base.OnFrameworkInitializationCompleted();
            }

            
        }
    }
}