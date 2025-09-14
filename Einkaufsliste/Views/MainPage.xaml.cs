using Einkaufsliste.Models;
using Einkaufsliste.ViewModels;
using Microsoft.Maui.ApplicationModel; // MainThread
using System.Linq;

namespace Einkaufsliste
{
    [QueryProperty(nameof(ListId), "listId")]
    [QueryProperty(nameof(ListName), "listName")]
    public partial class MainPage : ContentPage
    {
        private ShoppingListViewModel? _vm;
        private Guid? _currentListId;

        public string? ListId { get; set; }
        public string? ListName { get; set; }

        public MainPage()
        {
            InitializeComponent();
            // BindingContext wird gesetzt, sobald Parameter da sind
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();

            // Falls App ohne Parameter startet: nimm erste Liste
            if (string.IsNullOrWhiteSpace(ListId))
            {
                var index = new ViewModels.ListsIndexViewModel();
                var first = index.Lists.FirstOrDefault();
                if (first != null)
                {
                    ListId = first.Id.ToString();
                    ListName = first.Name;
                }
            }

            if (Guid.TryParse(ListId, out var gid))
            {
                // Neu bauen, wenn noch keine VM ODER wenn eine andere Liste angefordert wurde
                if (_vm == null || _currentListId != gid)
                {
                    _vm = new ShoppingListViewModel(gid, ListName ?? "Einkaufsliste");
                    _currentListId = gid;
                    BindingContext = _vm;
                    Title = _vm.Title; // falls du kein Binding verwenden willst
                }
            }
        }

        private void NewItemEntry_Completed(object sender, EventArgs e)
        {
            if (BindingContext is ShoppingListViewModel vm && vm.AddItemCommand.CanExecute(null))
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