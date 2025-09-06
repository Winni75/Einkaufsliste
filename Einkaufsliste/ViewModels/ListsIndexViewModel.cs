using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using Einkaufsliste.Models;

namespace Einkaufsliste.ViewModels
{
    public class ListsIndexViewModel : BindableObject
    {
        private const string IndexKey = "list_index_v1";

        public ObservableCollection<ListInfo> Lists { get; } = new();

        private ListInfo? _selected;
        public ListInfo? Selected
        {
            get => _selected;
            set { _selected = value; OnPropertyChanged(); }
        }

        public ICommand AddListCommand { get; }
        public ICommand DeleteListCommand { get; }
        public ICommand RenameListCommand { get; }
        public ICommand OpenListCommand { get; }

        public ListsIndexViewModel()
        {
            AddListCommand = new Command(async () => await AddListAsync());
            DeleteListCommand = new Command<ListInfo>(async li => await DeleteListAsync(li));
            RenameListCommand = new Command<ListInfo>(async li => await RenameListAsync(li));
            OpenListCommand = new Command<ListInfo>(async li => await OpenListAsync(li));

            LoadIndex();

            if (Lists.Count == 0)
            {
                Lists.Add(new ListInfo { Id = Guid.NewGuid(), Name = "Einkaufsliste" });
                SaveIndex();
            }
        }

        private void SaveIndex()
        {
            var json = JsonSerializer.Serialize(Lists);
            Preferences.Default.Set(IndexKey, json);
        }

        private void LoadIndex()
        {
            Lists.Clear();
            var json = Preferences.Default.Get(IndexKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var data = JsonSerializer.Deserialize<List<ListInfo>>(json) ?? new();
                foreach (var li in data) Lists.Add(li);
            }
            catch { /* ignorieren */ }
        }

        // -------- Hilfsfunktion: aktuelle Seite für Dialoge (ohne veraltetes Application.MainPage) --------
        private static Page? GetCurrentPage()
        {
            return Shell.Current?.CurrentPage
                   ?? Application.Current?.Windows.FirstOrDefault()?.Page;
        }

        private async Task AddListAsync()
        {
            var page = GetCurrentPage();
            if (page is null) return;

            string name = await page.DisplayPromptAsync(
                "Neue Liste", "Wie soll die Liste heißen?",
                accept: "OK", cancel: "Abbrechen",
                placeholder: "z. B. Rewe", maxLength: 60);

            if (string.IsNullOrWhiteSpace(name)) return;

            Lists.Add(new ListInfo { Id = Guid.NewGuid(), Name = name.Trim() });
            SaveIndex();
        }

        private async Task DeleteListAsync(ListInfo? li)
        {
            if (li == null) return;

            var page = GetCurrentPage();
            if (page is null) return;

            bool confirm = await page.DisplayAlert(
                "Liste löschen",
                $"„{li.Name}“ wirklich löschen? Alle Einträge dieser Liste werden entfernt.",
                "Löschen", "Abbrechen");

            if (!confirm) return;

            // Items der Liste entfernen (separat gespeichert)
            var itemKey = $"shopping_items_v1_{li.Id:N}";
            Preferences.Default.Remove(itemKey);

            Lists.Remove(li);
            SaveIndex();
        }

        private async Task RenameListAsync(ListInfo? li)
        {
            if (li == null) return;

            var page = GetCurrentPage();
            if (page is null) return;

            var name = await page.DisplayPromptAsync(
                "Liste umbenennen", "Neuer Name:",
                accept: "OK", cancel: "Abbrechen",
                initialValue: li.Name, maxLength: 60);

            if (string.IsNullOrWhiteSpace(name)) return;

            li.Name = name.Trim();
            // Änderungen an der Collection signalisieren (UI aktualisiert ItemTemplate)
            OnPropertyChanged(nameof(Lists));
            SaveIndex();
        }

        private async Task OpenListAsync(ListInfo? li)
        {
            if (li == null) return;
            var uri = $"ListPage?listId={li.Id}&listName={Uri.EscapeDataString(li.Name)}";
            await Shell.Current.GoToAsync(uri);
        }
    }
}