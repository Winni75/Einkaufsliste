using Einkaufsliste.ViewModels;

namespace Einkaufsliste
{
    public partial class ListsPage : ContentPage
    {
        private readonly ListsIndexViewModel _vm = new();

        public ListsPage()
        {
            InitializeComponent();
            BindingContext = _vm;
        }
    }
}