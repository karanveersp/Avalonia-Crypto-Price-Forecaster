using System;

namespace ForecasterGUI.ViewModels
{
    public class HomeViewModel : ViewModelBase
    {
        public string Greeting { get; }

        private string GetUseranme()
        {
            var un = Environment.UserName;
            return char.ToUpper(un[0]) + un.Substring(1);
        }
        
        public HomeViewModel()
        {
            Greeting = $"Welcome, {GetUseranme()}";
        }
    }
}