using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using ForecasterGUI.ViewModels;

namespace ForecasterGUI
{
    /*
     * Defines a data template which converts view models into views.
     * 
     * The Match(object data) returns whether data inherits from ViewModelBase.
     * If it returns true, then the Build(object data) method is called.
     *
     * The Build(object data) method takes the full name and replaces
     * ViewModel with View. It then tries to find a type by that name.
     * If it is found, then an instance of that type is created and
     * returned.
     *
     * When an instance of ContentControl (like Window) has its Content
     * property set to a non-control, it searches up a tree of controls for
     * a DataTemplate that matches the content data. If no other DataTemplate
     * matches the data, it will eventually reach the ViewLocator in Application.DataTemplates.
     *
     * The ViewLocator then returns an instance of the corresponding View.
     * 
     */
    public class ViewLocator : IDataTemplate
    {
        public IControl Build(object data)
        {
            var name = data.GetType().FullName!.Replace("ViewModel", "View");
            var type = Type.GetType(name);

            if (type != null)
            {
                return (Control)Activator.CreateInstance(type)!;
            }
            else
            {
                return new TextBlock { Text = "Not Found: " + name };
            }
        }

        public bool Match(object data)
        {
            return data is ViewModelBase;
        }
    }
}