using Einkaufsliste.Models;
using Microsoft.Maui.ApplicationModel; // MainThread
using Microsoft.Maui.Storage;          // Preferences
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;

namespace Einkaufsliste.ViewModels
{
    public class ShoppingListViewModel : BindableObject
    {
        private const string PrefKey = "shopping_items_v1";
        private const string PrefFontKey = "item_font_size";

        public ObservableCollection<ShoppingItem> Items { get; } = new();

        // ---------------- Schriftgröße ----------------
        private double _itemFontSize = 25;
        public double ItemFontSize
        {
            get => _itemFontSize;
            set
            {
                if (System.Math.Abs(_itemFontSize - value) > double.Epsilon)
                {
                    _itemFontSize = value;
                    OnPropertyChanged();

                    // 🔹 HIER: beim Setzen sofort die App-Resource aktualisieren
                    UpdateFontResource(_itemFontSize);

                    // 🔹 persistieren
                    Preferences.Default.Set(PrefFontKey, _itemFontSize);
                }
            }
        }

        // ---------------- Eingabe ----------------
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

        // ---------------- Commands ----------------
        public ICommand ShowInputCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveCompletedCommand { get; }
        public ICommand ToggleItemCommand { get; }

        private CancellationTokenSource? _saveCts;
        private bool _resortPending = false;
        private bool _isSorting = false;

        // ---------------- Konstruktor ----------------
        public ShoppingListViewModel()
        {
            // 🔹 gespeicherte Schriftgröße laden
            ItemFontSize = Preferences.Default.Get(PrefFontKey, 25.0);

            // 🔹 HIER: gleich zum Start die App-Resource setzen
            UpdateFontResource(ItemFontSize);

            ShowInputCommand = new Command(() => IsInputVisible = true);
            AddItemCommand = new Command(AddItem, CanAddItem);
            RemoveCompletedCommand = new Command(RemoveCompleted);
            ToggleItemCommand = new Command<ShoppingItem>(ToggleItem);

            Items.CollectionChanged += OnItemsCollectionChanged;

            Load();
            foreach (var it in Items)
                AttachItem(it);

            RequestResort();
        }

        // ---------- Hilfsmethode: App-Resource sicher setzen ----------
        private void UpdateFontResource(double value)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (Application.Current?.Resources is not null)
                    Application.Current.Resources["ItemFontSize"] = value;
            });
        }

        private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItemName);

        private void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName))
                return;

            Items.Add(new ShoppingItem { Name = NewItemName });
            NewItemName = string.Empty;
            IsInputVisible = false;

            RequestResort();
        }

        private void ToggleItem(ShoppingItem item)
        {
            if (item is null) return;
            item.IsCompleted = !item.IsCompleted;
            RequestResort();
        }

        private void RemoveCompleted()
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (Items[i].IsCompleted)
                    Items.RemoveAt(i);
            }
            RequestResort();
        }

        // ---------- Autosave ----------
        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
                foreach (ShoppingItem it in e.OldItems)
                    DetachItem(it);

            if (e.NewItems != null)
                foreach (ShoppingItem it in e.NewItems)
                    AttachItem(it);

            ScheduleSave();
        }

        private void AttachItem(ShoppingItem item) =>
            item.PropertyChanged += OnItemPropertyChanged;

        private void DetachItem(ShoppingItem item) =>
            item.PropertyChanged -= OnItemPropertyChanged;

        private void OnItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ShoppingItem.IsCompleted) ||
                e.PropertyName == nameof(ShoppingItem.Name))
            {
                RequestResort();
                ScheduleSave();
            }
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
                    if (!token.IsCancellationRequested)
                        Save();
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
            if (string.IsNullOrWhiteSpace(json))
                return;

            try
            {
                var list = JsonSerializer.Deserialize<List<ShoppingItem>>(json);
                if (list is null) return;

                Items.Clear();
                foreach (var it in list)
                    Items.Add(it);
            }
            catch
            {
                // defektes/altes Format ignorieren
            }
        }

        // ---------- Sortierung entkoppeln ----------
        private void RequestResort()
        {
            if (_resortPending) return;
            _resortPending = true;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _resortPending = false;
                if (_isSorting) return;
                _isSorting = true;
                try
                {
                    SortItems();
                }
                finally
                {
                    _isSorting = false;
                }
            });
        }

        private void SortItems()
        {
            var sorted = Items
                .OrderBy(i => i.IsCompleted)
                .ThenBy(i => i.Name, System.StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var desired = sorted[i];
                var currentIndex = Items.IndexOf(desired);
                if (currentIndex != i)
                    Items.Move(currentIndex, i);
            }
        }
    }
}