using Einkaufsliste.ViewModels;

namespace Einkaufsliste
{
    public partial class SettingsPage : ContentPage
    {
        public SettingsPage()
        {
            InitializeComponent();
            BindingContext = App.VM; // gleiche Instanz wie MainPage
        }
    }
}