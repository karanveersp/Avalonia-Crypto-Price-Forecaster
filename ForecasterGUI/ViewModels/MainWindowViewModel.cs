using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Runtime.Serialization;
using Avalonia.Controls.Notifications;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace ForecasterGUI.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private ViewModelBase? _content;
        public ViewModelBase? Content
        {
            get => _content;
            private set => this.RaiseAndSetIfChanged(ref _content, value);
        }

        // public void AddItem()
        // {
        //     var vm = new AddItemViewModel();
        //
        //     Observable.Merge(
        //             vm.Ok,
        //             vm.Cancel.Select(_ => null as TodoItem))
        //         .Take(1)
        //         .Subscribe(model =>
        //         {
        //             if (model != null)
        //             {
        //                 ListViewModel.Items.Add(model);
        //             }
        //
        //             Content = ListViewModel;
        //         });
        //     Content = vm; // transition to new view by setting new view model.
        // }

        private IManagedNotificationManager _notificationManager;

        public IManagedNotificationManager NotificationManager
        {
            get => _notificationManager;
            set => this.RaiseAndSetIfChanged(ref _notificationManager, value);
        }
        
        public MainWindowViewModel(IManagedNotificationManager notificationManager)
        {
            _notificationManager = notificationManager;
            
            Content = new NavViewModel();
        }
        
    }
}
