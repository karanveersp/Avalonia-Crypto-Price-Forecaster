using System;
using System.Reactive.Linq;
using System.Runtime.Serialization;
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
        
        public MainWindowViewModel()
        {
            Content = new NavViewModel();
        }
        
    }
}
