using Einkaufsliste.ViewModels;

namespace Einkaufsliste
{
    public partial class App : Application
    {
        // Gemeinsames ViewModel für alle Seiten – aber erst NACH InitializeComponent erzeugen
        public static ShoppingListViewModel VM { get; private set; } = null!;

        public App()
        {
            InitializeComponent();

            VM = new ShoppingListViewModel();   // Jetzt ist Application.Current verfügbar
            MainPage = new AppShell();          // deine Shell mit Flyout
        }
    }
}