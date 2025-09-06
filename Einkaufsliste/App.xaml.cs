namespace Einkaufsliste
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            // Shell als Root-Page
            return new Window(new AppShell());
        }
    }
}