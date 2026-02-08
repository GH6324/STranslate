using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using ObservableCollections;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using System.IO;
using System.Text;
using System.Text.Json;

namespace STranslate.ViewModels.Pages;

public partial class HistoryViewModel : ObservableObject, IDisposable
{
    private const int PageSize = 20;
    private const int searchDelayMilliseconds = 500;

    private readonly SqlService _sqlService;
    private readonly ISnackbar _snackbar;
    private readonly Internationalization _i18n;
    private readonly DebounceExecutor _searchDebouncer;

    private CancellationTokenSource? _searchCts;
    private DateTime _lastCursorTime = DateTime.Now;
    private bool _isLoading = false;

    private bool CanLoadMore =>
        !_isLoading &&
        string.IsNullOrEmpty(SearchText) &&
        (TotalCount == 0 || _items.Count != TotalCount);

    [ObservableProperty] public partial string SearchText { get; set; } = string.Empty;

    /// <summary>
    /// <see href="https://blog.coldwind.top/posts/more-observable-collections/"/>
    /// </summary>
    private readonly ObservableList<HistoryModel> _items = [];

    public INotifyCollectionChangedSynchronizedViewList<HistoryModel> HistoryItems { get; }

    [ObservableProperty] public partial HistoryModel? SelectedListItem { get; set; }

    [ObservableProperty] public partial ObservableList<object> SelectedItems { get; set; } = [];

    [ObservableProperty] public partial HistoryModel? SelectedItem { get; set; }

    [ObservableProperty] public partial long TotalCount { get; set; }

    public bool CanExportHistory => SelectedItems.Count > 0;

    public HistoryViewModel(
        SqlService sqlService,
        ISnackbar snackbar,
        Internationalization i18n)
    {
        _sqlService = sqlService;
        _snackbar = snackbar;
        _i18n = i18n;
        _searchDebouncer = new();

        HistoryItems = _items.ToNotifyCollectionChanged();
        SelectedItems.CollectionChanged += (in NotifyCollectionChangedEventArgs<object> _) => OnPropertyChanged(nameof(CanExportHistory));

        _ = RefreshAsync();
    }

    partial void OnSelectedListItemChanged(HistoryModel? value) => SelectedItem = value;

    // 搜索文本变化时修改定时器
    partial void OnSearchTextChanged(string value) =>
        _searchDebouncer.ExecuteAsync(SearchAsync, TimeSpan.FromMilliseconds(searchDelayMilliseconds));

    private async Task SearchAsync()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();

        if (string.IsNullOrEmpty(SearchText))
        {
            await RefreshAsync();
            return;
        }

        var historyItems = await _sqlService.GetDataAsync(SearchText, _searchCts.Token);

        App.Current.Dispatcher.Invoke(() =>
        {
            SelectedListItem = null;
            SelectedItem = null;
            ClearItems();
            if (historyItems == null) return;

            AddItems(historyItems);
        });
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        TotalCount = await _sqlService.GetCountAsync();

        App.Current.Dispatcher.Invoke(() =>
        {
            SelectedListItem = null;
            SelectedItem = null;
            ClearItems();
        });
        _lastCursorTime = DateTime.Now;

        await LoadMoreAsync();
    }

    [RelayCommand]
    private async Task DeleteAsync(HistoryModel historyModel)
    {
        if (await new ContentDialog
        {
            Title = _i18n.GetTranslation("Prompt"),
            CloseButtonText = _i18n.GetTranslation("Cancel"),
            PrimaryButtonText = _i18n.GetTranslation("Confirm"),
            DefaultButton = ContentDialogButton.Primary,
            Content = string.Format(_i18n.GetTranslation("BatchDeleteHistoryConfirm"), "1"),
        }.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }
        var success = await _sqlService.DeleteDataAsync(historyModel);
        if (success)
        {
            App.Current.Dispatcher.Invoke(() =>
            {
                var item = _items.FirstOrDefault(i => i.Id == historyModel.Id);
                if (item != null)
                    RemoveItem(item);
                if (SelectedItem?.Id == historyModel.Id)
                {
                    SelectedListItem = null;
                    SelectedItem = null;
                }
            });
            TotalCount--;
        }
        else
            _snackbar.ShowError(_i18n.GetTranslation("OperationFailed"));
    }

    [RelayCommand]
    private void Copy(string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        ClipboardHelper.SetText(text);
        _snackbar.ShowSuccess(_i18n.GetTranslation("CopySuccess"));
    }

    [RelayCommand(CanExecute = nameof(CanLoadMore))]
    private async Task LoadMoreAsync()
    {
        try
        {
            _isLoading = true;

            var historyData = await _sqlService.GetDataCursorPagedAsync(PageSize, _lastCursorTime);
            if (!historyData.Any()) return;

            App.Current.Dispatcher.Invoke(() =>
            {
                // 更新游标
                _lastCursorTime = historyData.Last().Time;
                var uniqueHistoryItems = historyData.Where(h => !_items.Any(existing => existing.Id == h.Id));
                AddItems(uniqueHistoryItems);
            });
        }
        finally
        {
            _isLoading = false;
            LoadMoreCommand.NotifyCanExecuteChanged();
        }
    }

    [RelayCommand]
    private void ToggleSelectAll()
    {
        if (_items.Count == 0) return;

        if (SelectedItems.Count == _items.Count)
            SelectedItems.Clear();
        else
        {
            SelectedItems.Clear();
            SelectedItems.AddRange(_items);
        }
    }

    [RelayCommand]
    private async Task ExportHistoryAsync()
    {
        var selected = SelectedItems.Cast<HistoryModel>().ToList();

        if (selected.Count == 0)
        {
            _snackbar.Show(
                _i18n.GetTranslation("NoHistorySelected"),
                Severity.Warning,
                actionText: _i18n.GetTranslation("SelectAll"),
                actionCallback: ToggleSelectAll);
            return;
        }

        var saveFileDialog = new SaveFileDialog
        {
            Title = _i18n.GetTranslation("SaveAs"),
            Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
            FileName = $"stranslate_history_{DateTime.Now:yyyyMMddHHmmss}.json",
            DefaultDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            AddToRecent = true
        };

        if (saveFileDialog.ShowDialog() != true)
            return;

        try
        {
            var export = new
            {
                app = Constant.AppName,
                exportedAt = DateTimeOffset.Now,
                count = selected.Count,
                items = selected.Select(h => new
                {
                    id = h.Id,
                    time = h.Time,
                    sourceLang = h.SourceLang,
                    targetLang = h.TargetLang,
                    sourceText = h.SourceText,
                    favorite = h.Favorite,
                    remark = h.Remark,
                    data = h.Data
                })
            };

            var json = JsonSerializer.Serialize(export, HistoryModel.JsonOption);
            await File.WriteAllTextAsync(saveFileDialog.FileName, json, Encoding.UTF8);

            var directory = Path.GetDirectoryName(saveFileDialog.FileName);
            _snackbar.ShowSuccess(_i18n.GetTranslation("ExportSuccess"));

            SelectedItems.Clear();
        }
        catch (Exception ex)
        {
            _snackbar.ShowError($"{_i18n.GetTranslation("ExportFailed")}: {ex.Message}");
        }
    }

    [RelayCommand]
    private async Task DeleteSelectedHistoryAsync()
    {
        var selected = SelectedItems.Cast<HistoryModel>().ToList();
        if (selected.Count == 0)
        {
            _snackbar.Show(
                _i18n.GetTranslation("NoHistorySelected"),
                Severity.Warning,
                actionText: _i18n.GetTranslation("SelectAll"),
                actionCallback: ToggleSelectAll);
            return;
        }

        if (await new ContentDialog
        {
            Title = _i18n.GetTranslation("Prompt"),
            CloseButtonText = _i18n.GetTranslation("Cancel"),
            PrimaryButtonText = _i18n.GetTranslation("Confirm"),
            DefaultButton = ContentDialogButton.Primary,
            Content = string.Format(_i18n.GetTranslation("BatchDeleteHistoryConfirm"), selected.Count),
        }.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        var totalCountBefore = TotalCount;
        foreach (var item in selected)
            await DeleteAsync(item);

        if (TotalCount == totalCountBefore - selected.Count)
            _snackbar.ShowSuccess(_i18n.GetTranslation("OperationSuccess"));
    }

    private void AddItems(IEnumerable<HistoryModel> models)
    {
        _items.AddRange(models);
    }

    private void RemoveItem(HistoryModel item)
    {
        _items.Remove(item);
        SelectedItems.Remove(item);
    }

    private void ClearItems()
    {
        _items.Clear();
        SelectedItems.Clear();
    }

    public void Dispose()
    {
        _searchDebouncer.Dispose();
        _searchCts?.Dispose();
    }
}
