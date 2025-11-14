using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Instances;
using STranslate.Plugin;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace STranslate.ViewModels.Pages;

public partial class PluginViewModel : ObservableObject
{
    private readonly PluginInstance _pluginInstance;
    private readonly Internationalization _i18n;

    public DataProvider DataProvider { get; }

    private readonly ISnackbar _snackbar;
    private readonly Settings _settings;
    private readonly CollectionViewSource _pluginCollectionView;
    public ICollectionView PluginCollectionView => _pluginCollectionView.View;

    [ObservableProperty] public partial string FilterText { get; set; } = string.Empty;

    [ObservableProperty] public partial ListSortDirection NameSortDirection { get; set; } = ListSortDirection.Ascending;
    [ObservableProperty] public partial ListSortDirection VersionSortDirection { get; set; } = ListSortDirection.Ascending;

    public PluginViewModel(
        PluginInstance pluginInstance,
        Internationalization i18n,
        DataProvider dataProvider,
        ISnackbar snackbar,
        Settings settings
        )
    {
        _pluginInstance = pluginInstance;
        _i18n = i18n;
        DataProvider = dataProvider;
        _snackbar = snackbar;
        _settings = settings;

        _pluginCollectionView = new()
        {
            Source = _pluginInstance.PluginMetaDatas
        };
        _pluginCollectionView.Filter += OnPluginFilter;
    }

    [ObservableProperty]
    public partial PluginType PluginType { get; set; } = PluginType.All;

    private void OnPluginFilter(object sender, FilterEventArgs e)
    {
        if (e.Item is not PluginMetaData plugin)
        {
            e.Accepted = false;
            return;
        }

        // 类型筛选
        var typeMatch = PluginType switch
        {
            PluginType.Translate => typeof(ITranslatePlugin).IsAssignableFrom(plugin.PluginType) || typeof(IDictionaryPlugin).IsAssignableFrom(plugin.PluginType),
            PluginType.Ocr => typeof(IOcrPlugin).IsAssignableFrom(plugin.PluginType),
            PluginType.Tts => typeof(ITtsPlugin).IsAssignableFrom(plugin.PluginType),
            PluginType.Vocabulary => typeof(IVocabularyPlugin).IsAssignableFrom(plugin.PluginType),
            _ => true,
        };

        // 文本筛选
        var textMatch = string.IsNullOrEmpty(FilterText)
            || plugin.Name.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || plugin.Author.Contains(FilterText, StringComparison.OrdinalIgnoreCase)
            || plugin.Description.Contains(FilterText, StringComparison.OrdinalIgnoreCase);

        e.Accepted = typeMatch && textMatch;
    }

    private void ApplySort(string propertyName, ListSortDirection direction)
    {
        _pluginCollectionView.SortDescriptions.Clear();
        _pluginCollectionView.SortDescriptions.Add(new SortDescription(propertyName, direction));
    }

    private void ApplySortWithCustomComparer(ListSortDirection direction, IComparer comparer)
    {
        _pluginCollectionView.SortDescriptions.Clear();

        if (_pluginCollectionView.View is ListCollectionView listView)
        {
            if (direction == ListSortDirection.Descending)
            {
                listView.CustomSort = new ReverseComparer(comparer);
            }
            else
            {
                listView.CustomSort = comparer;
            }
        }
    }

    [RelayCommand]
    private void SortByName()
    {
        NameSortDirection = NameSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        ApplySort(nameof(PluginMetaData.Name), NameSortDirection);
    }

    [RelayCommand]
    private void SortByVersion()
    {
        VersionSortDirection = VersionSortDirection == ListSortDirection.Ascending ? ListSortDirection.Descending : ListSortDirection.Ascending;
        ApplySortWithCustomComparer(VersionSortDirection, new VersionComparer());
    }

    partial void OnPluginTypeChanged(PluginType value) => _pluginCollectionView.View?.Refresh();

    partial void OnFilterTextChanged(string value) => _pluginCollectionView.View?.Refresh();

    [RelayCommand]
    private async Task AddPluginAsync()
    {
        // Open a file dialog to select a plugin zip file
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Title = _i18n.GetTranslation("SelectPluginFile"),
            Filter = "Spkg File (*.spkg)|*.spkg",
            Multiselect = false,
            RestoreDirectory = true
        };
        if (dialog.ShowDialog() != true)
        {
            return; // User canceled the dialog
        }
        var spkgPluginFilePath = dialog.FileName;
        var installResult = _pluginInstance.InstallPlugin(spkgPluginFilePath);

        if (installResult.RequiredUpgrade && installResult.ExistingPlugin != null)
        {
            // 插件已存在，询问是否升级
            var result = await new ContentDialog
            {
                Title = _i18n.GetTranslation("PluginUpgrade"),
                Content = string.Format(_i18n.GetTranslation("PluginUpgradeConfirm"), installResult.ExistingPlugin.Name, installResult.ExistingPlugin.Version, installResult.NewPlugin?.Version),
                PrimaryButtonText = _i18n.GetTranslation("Confirm"),
                CloseButtonText = _i18n.GetTranslation("Cancel"),
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // 执行升级
                if (_pluginInstance.UpgradePlugin(installResult.ExistingPlugin, spkgPluginFilePath))
                {
                    var restartResult = await new ContentDialog
                    {
                        Title = _i18n.GetTranslation("Prompt"),
                        Content = _i18n.GetTranslation("PluginUpgradeSuccess"),
                        PrimaryButtonText = _i18n.GetTranslation("Confirm"),
                        CloseButtonText = _i18n.GetTranslation("Cancel"),
                        DefaultButton = ContentDialogButton.Primary,
                    }.ShowAsync();

                    if (restartResult == ContentDialogResult.Primary)
                    {
                        UACHelper.Run(_settings.StartMode);
                        App.Current.Shutdown();
                    }
                }
                else
                {
                    _snackbar.ShowError(_i18n.GetTranslation("PluginUpgradeFailed"));
                }
            }
        }
        else if (!installResult.Succeeded)
        {
            _ = new ContentDialog
            {
                Title = _i18n.GetTranslation("PluginInstallFailed"),
                CloseButtonText = _i18n.GetTranslation("Ok"),
                DefaultButton = ContentDialogButton.Close,
                Content = installResult.Message
            }.ShowAsync().ConfigureAwait(false);
        }
        else
        {
            _snackbar.ShowSuccess(_i18n.GetTranslation("PluginInstallSuccess"));
        }
    }

    [RelayCommand]
    private async Task PluginSummaryAsync(Control control)
    {
        var helpDialog = new ContentDialog()
        {
            Owner = Window.GetWindow(control),
            Content = new StackPanel
            {
                Children =
                {
                    GetTextBlock("PluginTypeAll", _pluginInstance.PluginMetaDatas.Count.ToString(), new Thickness()),
                    GetTextBlock("PluginTypeTranslate", _pluginInstance.PluginMetaDatas.Where(x => typeof(ITranslatePlugin).IsAssignableFrom(x.PluginType) || typeof(IDictionaryPlugin).IsAssignableFrom(x.PluginType)).Count().ToString(), new Thickness(0, 24, 0, 10)),
                    GetTextBlock("PluginTypeOcr", _pluginInstance.PluginMetaDatas.Where(x => typeof(IOcrPlugin).IsAssignableFrom(x.PluginType)).Count().ToString(), new Thickness(0, 24, 0, 10)),
                    GetTextBlock("PluginTypeTts", _pluginInstance.PluginMetaDatas.Where(x => typeof(ITtsPlugin).IsAssignableFrom(x.PluginType)).Count().ToString(), new Thickness(0, 24, 0, 10)),
                    GetTextBlock("PluginTypeVocabulary", _pluginInstance.PluginMetaDatas.Where(x => typeof(IVocabularyPlugin).IsAssignableFrom(x.PluginType)).Count().ToString(), new Thickness(0, 24, 0, 0)),
                }
            },
            PrimaryButtonText = (string)Application.Current.Resources["Ok"],
            DefaultButton = ContentDialogButton.Primary,
            CornerRadius = new CornerRadius(8),
            Style = (Style)Application.Current.Resources["ContentDialog"]
        };

        await helpDialog.ShowAsync();

        TextBlock GetTextBlock(string resourceKey, string text, Thickness thickness) =>
            new()
            {
                Text = $"{(string)Application.Current.Resources[resourceKey]}: {text}",
                FontSize = 16,
                TextWrapping = TextWrapping.Wrap,
                Margin = thickness
            };
    }

    [RelayCommand]
    private void OpenPluginDirectory(PluginMetaData plugin)
    {
        var directory = plugin.PluginDirectory;
        if (!string.IsNullOrEmpty(directory))
            Process.Start("explorer.exe", directory);
    }

    [RelayCommand]
    private async Task DeletePluginAsync(PluginMetaData plugin)
    {
        if (await new ContentDialog
        {
            Title = _i18n.GetTranslation("Prompt"),
            CloseButtonText = _i18n.GetTranslation("Cancel"),
            PrimaryButtonText = _i18n.GetTranslation("Confirm"),
            DefaultButton = ContentDialogButton.Primary,
            Content = string.Format(_i18n.GetTranslation("PluginDeleteConfirm"), plugin.Author, plugin.Version, plugin.Name),
        }.ShowAsync() != ContentDialogResult.Primary)
        {
            return;
        }

        if (!_pluginInstance.UninstallPlugin(plugin))
        {
            _ = new ContentDialog
            {
                Title = _i18n.GetTranslation("Prompt"),
                CloseButtonText = _i18n.GetTranslation("Ok"),
                DefaultButton = ContentDialogButton.Close,
                Content = _i18n.GetTranslation("PluginDeleteFailed")
            }.ShowAsync().ConfigureAwait(false);

            return;
        }

        if (await new ContentDialog
        {
            Title = _i18n.GetTranslation("Prompt"),
            CloseButtonText = _i18n.GetTranslation("Cancel"),
            PrimaryButtonText = _i18n.GetTranslation("Confirm"),
            DefaultButton = ContentDialogButton.Primary,
            Content = _i18n.GetTranslation("PluginDeleteForRestart"),
        }.ShowAsync() == ContentDialogResult.Primary)
        {
            UACHelper.Run(_settings.StartMode);
            App.Current.Shutdown();
        }
    }

    [RelayCommand]
    private void OpenOfficialLink(string url)
        => Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
}

/// <summary>
/// 版本号比较器，支持语义化版本排序
///     * 1.0.0
///     * 2.1.3.4
///     * 1.0.0-beta
///     * 1.2
///     * v1.0.0（会自动清理非数字字符）
/// </summary>
public class VersionComparer : IComparer
{
    public int Compare(object? x, object? y)
    {
        if (x is not PluginMetaData pluginX || y is not PluginMetaData pluginY)
            return 0;

        return CompareVersions(pluginX.Version, pluginY.Version);
    }

    private static int CompareVersions(string version1, string version2)
    {
        if (string.IsNullOrEmpty(version1) && string.IsNullOrEmpty(version2)) return 0;
        if (string.IsNullOrEmpty(version1)) return -1;
        if (string.IsNullOrEmpty(version2)) return 1;

        // 尝试解析为 System.Version
        if (Version.TryParse(version1, out var v1) && Version.TryParse(version2, out var v2))
        {
            return v1.CompareTo(v2);
        }

        // 手动解析版本号（支持更灵活的格式）
        var parts1 = ParseVersionParts(version1);
        var parts2 = ParseVersionParts(version2);

        int maxLength = Math.Max(parts1.Length, parts2.Length);
        for (int i = 0; i < maxLength; i++)
        {
            int part1 = i < parts1.Length ? parts1[i] : 0;
            int part2 = i < parts2.Length ? parts2[i] : 0;

            int result = part1.CompareTo(part2);
            if (result != 0) return result;
        }

        return 0;
    }

    private static int[] ParseVersionParts(string version)
    {
        // 移除非数字字符，只保留数字和点
        var cleanVersion = new string(version.Where(c => char.IsDigit(c) || c == '.').ToArray());

        return cleanVersion.Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => int.TryParse(part, out var num) ? num : 0)
            .ToArray();
    }
}

/// <summary>
/// 反向比较器包装器
/// </summary>
public class ReverseComparer : IComparer
{
    private readonly IComparer _innerComparer;

    public ReverseComparer(IComparer innerComparer)
    {
        _innerComparer = innerComparer;
    }

    public int Compare(object? x, object? y) => _innerComparer.Compare(y, x);
}

public enum PluginType
{
    All,
    Translate,
    Ocr,
    Tts,
    Vocabulary,
}