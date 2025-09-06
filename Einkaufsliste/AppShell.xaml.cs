namespace Einkaufsliste
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("ListPage", typeof(MainPage));
        }
    }
}
