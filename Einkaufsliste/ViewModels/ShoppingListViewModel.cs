using Einkaufsliste.Models;
using Microsoft.Maui.ApplicationModel;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Text.Json;
using System.Windows.Input;

namespace Einkaufsliste.ViewModels
{
    public class ShoppingListViewModel : BindableObject
    {
        private readonly Guid _listId;
        private readonly string _prefKey; // pro Liste einzigartig
        public string Title { get; private set; } = "Einkaufsliste";

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
            set { if (_isInputVisible != value) { _isInputVisible = value; OnPropertyChanged(); } }
        }

        public ICommand ShowInputCommand { get; }
        public ICommand AddItemCommand { get; }
        public ICommand RemoveCompletedCommand { get; }
        public ICommand ToggleItemCommand { get; }

        private CancellationTokenSource? _saveCts;
        private bool _resortPending = false;
        private bool _isSorting = false;

        public ShoppingListViewModel(Guid listId, string listName)
        {
            _listId = listId;
            Title = string.IsNullOrWhiteSpace(listName) ? "Einkaufsliste" : listName.Trim();
            _prefKey = $"shopping_items_v1_{_listId:N}";

            ShowInputCommand = new Command(() => IsInputVisible = true);
            AddItemCommand = new Command(AddItem, CanAddItem);
            RemoveCompletedCommand = new Command(RemoveCompleted);
            ToggleItemCommand = new Command<ShoppingItem>(ToggleItem);

            Items.CollectionChanged += OnItemsCollectionChanged;

            Load();
            foreach (var it in Items) AttachItem(it);
            RequestResort();
        }

        private bool CanAddItem() => !string.IsNullOrWhiteSpace(NewItemName);

        private void AddItem()
        {
            if (string.IsNullOrWhiteSpace(NewItemName)) return;

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
                if (Items[i].IsCompleted)
                    Items.RemoveAt(i);
            RequestResort();
        }

        private void OnItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null) foreach (ShoppingItem it in e.OldItems) DetachItem(it);
            if (e.NewItems != null) foreach (ShoppingItem it in e.NewItems) AttachItem(it);
            ScheduleSave();
        }

        private void AttachItem(ShoppingItem item) => item.PropertyChanged += OnItemPropertyChanged;
        private void DetachItem(ShoppingItem item) => item.PropertyChanged -= OnItemPropertyChanged;

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
                    if (!token.IsCancellationRequested) Save();
                }
                catch (TaskCanceledException) { }
            }, token);
        }

        private void Save()
        {
            var list = Items.ToList();
            var json = JsonSerializer.Serialize(list);
            Preferences.Default.Set(_prefKey, json);
        }

        private void Load()
        {
            var json = Preferences.Default.Get(_prefKey, string.Empty);
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                var list = JsonSerializer.Deserialize<List<ShoppingItem>>(json);
                if (list is null) return;

                Items.Clear();
                foreach (var it in list) Items.Add(it);
            }
            catch { /* altes/defektes Format ignorieren */ }
        }

        private void RequestResort()
        {
            if (_resortPending) return;
            _resortPending = true;

            MainThread.BeginInvokeOnMainThread(() =>
            {
                _resortPending = false;
                if (_isSorting) return;
                _isSorting = true;
                try { SortItems(); }
                finally { _isSorting = false; }
            });
        }

        private void SortItems()
        {
            var sorted = Items
                .OrderBy(i => i.IsCompleted)
                .ThenBy(i => i.Name, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            for (int i = 0; i < sorted.Count; i++)
            {
                var currentIndex = Items.IndexOf(sorted[i]);
                if (currentIndex != i) Items.Move(currentIndex, i);
            }
        }
    }
}