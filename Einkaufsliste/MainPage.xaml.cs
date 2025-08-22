using Einkaufsliste.ViewModels;

namespace Einkaufsliste
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = new ShoppingListViewModel();
        }

        // Enter im Entry -> AddItem ausführen (ViewModel schließt danach das Eingabefeld)
        private void NewItemEntry_Completed(object sender, EventArgs e)
        {
            if (BindingContext is ShoppingListViewModel vm)
            {
                if (vm.AddItemCommand.CanExecute(null))
                    vm.AddItemCommand.Execute(null);
            }
        }

        // Nach Klick auf +: Eingabefeld sichtbar machen & Fokus setzen
        private void PlusButton_Clicked(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);    // sicherstellen, dass IsInputVisible true ist
                NewItemEntry?.Focus();
            });
        }
    }
}
