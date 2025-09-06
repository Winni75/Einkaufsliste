using System.Windows.Input;

namespace Einkaufsliste.ViewModels
{
    public class SettingsViewModel : BindableObject
    {
        private const string FontPrefKey = "font_size_v1";

        private double _itemFontSize;
        public double ItemFontSize
        {
            get => _itemFontSize;
            set
            {
                if (_itemFontSize != value)
                {
                    _itemFontSize = value;
                    OnPropertyChanged();
                    // sofort in die App-Resource schreiben -> MainPage-Labels (DynamicResource) aktualisieren sich
                    ApplyToResources();
                    // persistent speichern
                    Preferences.Default.Set(FontPrefKey, _itemFontSize);
                }
            }
        }

        public ICommand IncreaseFontCommand { get; }
        public ICommand DecreaseFontCommand { get; }

        public SettingsViewModel()
        {
            // gespeicherten Wert laden (Default 25)
            ItemFontSize = Preferences.Default.Get(FontPrefKey, 25.0);
            ApplyToResources();

            IncreaseFontCommand = new Command(() => ItemFontSize += 2);
            DecreaseFontCommand = new Command(() => ItemFontSize = Math.Max(12, ItemFontSize - 2));
        }

        private void ApplyToResources()
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current?.Resources != null)
                    Application.Current.Resources["ItemFontSize"] = ItemFontSize;
            });
        }
    }
}
