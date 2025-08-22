using Einkaufsliste.Models;
using Einkaufsliste.ViewModels;

namespace Einkaufsliste
{
    public partial class MainPage : ContentPage
    {
        public MainPage()
        {
            InitializeComponent();
            BindingContext = App.VM; // gemeinsames VM
        }

        private void NewItemEntry_Completed(object sender, EventArgs e)
        {
            if (BindingContext is ShoppingListViewModel vm)
                if (vm.AddItemCommand.CanExecute(null))
                    vm.AddItemCommand.Execute(null);
        }

        private void PlusButton_Clicked(object sender, EventArgs e)
        {
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                await Task.Delay(50);
                NewItemEntry?.Focus();
            });
        }

        // Tippen auf eine Zeile (SelectionChanged) toggelt IsCompleted
        private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (e.CurrentSelection?.FirstOrDefault() is ShoppingItem item &&
                    BindingContext is ShoppingListViewModel vm &&
                    (vm.ToggleItemCommand as Command<ShoppingItem>)?.CanExecute(item) == true)
                {
                    vm.ToggleItemCommand.Execute(item);
                }
            }
            finally
            {
                if (sender is CollectionView cv) cv.SelectedItem = null;
            }
        }
    }
}