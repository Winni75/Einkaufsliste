using Einkaufsliste.Models;
using Microsoft.Maui.Storage;                 // Preferences.Default
using System.Collections.ObjectModel;
using System.Collections.Specialized;        // INotifyCollectionChanged
using System.ComponentModel;                 // INotifyPropertyChanged
using System.Linq;                           // OrderBy, ToList
using System.Text.Json;                      // JSON
using System.Threading;                      // CancellationTokenSource
using System.Threading.Tasks;                // Task
using System.Windows.Input;                  // ICommand

namespace Einkaufsliste.ViewModels
{
    public class ShoppingListViewModel : BindableObject
    {
        private const string PrefKey = "shopping_items_v1";

        public ObservableCollection<ShoppingItem> Items { get; } = new();

        private string _newItemName = string.Empty;
        public string NewItemName
        {
            get => _newItemName;
            set
            {
                if (_newItemName != value)
                {
                    _newItemName = value;
                    OnPropertyChanged();
                    (AddItemCommand as Command)?.ChangeCanExecute();
                }
            }
        }

        private bool _isInputVisible;
        public bool IsInputVisible
        {
            get => _isInputVisible;
            set
            {
                if (_isInputVisible != value)
                {
                    _isInputVisible = value;
                    OnPropertyChanged();
                }
            }
        }

        public ICommand ShowInputCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveCompletedCommand { get; }
        public ICommand ToggleItemCommand { get; }

        private CancellationTokenSource? _saveCts;

        public ShoppingListViewModel()
        {
            ShowInputCommand = new Command(() => IsInputVisible = true);
            AddItemCommand = new Command(AddItem, CanAddItem);
            RemoveCompletedCommand = new Command(RemoveCompleted);
            ToggleItemCommand = new Command<ShoppingItem>(ToggleItem);

            Items.CollectionChanged += OnItemsCollectionChanged;

            Load();
            foreach (var it in Items)
                AttachItem(it);

            SortItems(); // beim Start sortieren
        }

        private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItemName);

        private void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName))
                return;

            Items.Add(new ShoppingItem { Name = NewItemName });
            NewItemName = string.Empty; // Eingabefeld leeren
            IsInputVisible = false;        // Eingabeleiste schließen
            SortItems();
        }

        private void ToggleItem(ShoppingItem item)
        {
            if (item is null) return;
            item.IsCompleted = !item.IsCompleted;
            SortItems();
        }

        private void RemoveCompleted()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].IsCompleted)
                    Items.RemoveAt(i);
            }
            // Save folgt über CollectionChanged
        }

        // --- Sortierung: offene oben, erledigte unten ---
        private void SortItems()
        {
            var sorted = Items.OrderBy(i => i.IsCompleted).ToList();
            for (int targetIndex = 0; targetIndex < sorted.Count; targetIndex++)
            {
                var desired = sorted[targetIndex];
                var currentIndex = Items.IndexOf(desired);
                if (currentIndex != targetIndex)
                    Items.Move(currentIndex, targetIndex);
            }
            ScheduleSave();
        }

        // --- Autosave ---
        private void OnItemsCollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (ShoppingItem it in e.OldItems) DetachItem(it);

            if (e.NewItems != null)
                foreach (ShoppingItem it in e.NewItems) AttachItem(it);

            ScheduleSave();
        }

        private void AttachItem(ShoppingItem item) =>
            item.PropertyChanged += OnItemPropertyChanged;

        private void DetachItem(ShoppingItem item) =>
            item.PropertyChanged -= OnItemPropertyChanged;

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName is nameof(ShoppingItem.IsCompleted) or nameof(ShoppingItem.Name))
                ScheduleSave();
        }

        private void ScheduleSave(int delayMs = 300)
        {
            _saveCts?.Cancel();
            _saveCts = new CancellationTokenSource();
            var token = _saveCts.Token;

            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(delayMs, token);
                    if (!token.IsCancellationRequested) Save();
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void Save()
        {
            var list = Items.ToList();
            var json = JsonSerializer.Serialize(list);
            Preferences.Default.Set(PrefKey, json);
        }

        private void Load()
        {
            var json = Preferences.Default.Get(PrefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var list = JsonSerializer.Deserialize<List<ShoppingItem>>(json);
                if (list is null) return;

                Items.Clear();
                foreach (var it in list) Items.Add(it);
            }
            catch
            {
                // defektes/altes Format ignorieren
            }
        }
    }
}