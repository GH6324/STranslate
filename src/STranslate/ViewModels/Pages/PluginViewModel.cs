using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using iNKORE.UI.WPF.Modern.Controls;
using Microsoft.Win32;
using STranslate.Core;
using STranslate.Helpers;
using STranslate.Plugin;
using STranslate.Services;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Data;

namespace STranslate.ViewModels.Pages;

public partial class PluginViewModel : ObservableObject
{
    private readonly PluginService _pluginService;
    private readonly Internationalization _i18n;

    public DataProvider DataProvider { get; }

    private readonly ISnackbar _snackbar;
    private readonly Settings _settings;
    private readonly CollectionViewSource _pluginCollectionView;
    public ICollectionView PluginCollectionView => _pluginCollectionView.View;

    [ObservableProperty] public partial string FilterText { get; set; } = string.Empty;

    /// <summary>
    /// 所有插件数量
    /// </summary>
    public int TotalPluginCount => _pluginService.PluginMetaDatas.Count;

    /// <summary>
    /// 翻译插件数量（包含翻译和词典插件）
    /// </summary>
    public int TranslatePluginCount => _pluginService.PluginMetaDatas
        .Where(x => typeof(ITranslatePlugin).IsAssignableFrom(x.PluginType) || typeof(IDictionaryPlugin).IsAssignableFrom(x.PluginType))
        .Count();

    /// <summary>
    /// OCR插件数量
    /// </summary>
    public int OcrPluginCount => _pluginService.PluginMetaDatas
        .Where(x => typeof(IOcrPlugin).IsAssignableFrom(x.PluginType))
        .Count();

    /// <summary>
    /// TTS插件数量
    /// </summary>
    public int TtsPluginCount => _pluginService.PluginMetaDatas
        .Where(x => typeof(ITtsPlugin).IsAssignableFrom(x.PluginType))
        .Count();

    /// <summary>
    /// 词汇表插件数量
    /// </summary>
    public int VocabularyPluginCount => _pluginService.PluginMetaDatas
        .Where(x => typeof(IVocabularyPlugin).IsAssignableFrom(x.PluginType))
        .Count();

    public PluginViewModel(
        PluginService pluginService,
        Internationalization i18n,
        DataProvider dataProvider,
        ISnackbar snackbar,
        Settings settings
        )
    {
        _pluginService = pluginService;
        _i18n = i18n;
        DataProvider = dataProvider;
        _snackbar = snackbar;
        _settings = settings;

        _pluginCollectionView = new()
        {
            Source = _pluginService.PluginMetaDatas
        };
        _pluginCollectionView.Filter += OnPluginFilter;

        // 监听插件集合变化，更新计数
        _pluginService.PluginMetaDatas.CollectionChanged += (s, e) =>
        {
            OnPropertyChanged(nameof(TotalPluginCount));
            OnPropertyChanged(nameof(TranslatePluginCount));
            OnPropertyChanged(nameof(OcrPluginCount));
            OnPropertyChanged(nameof(TtsPluginCount));
            OnPropertyChanged(nameof(VocabularyPluginCount));
        };
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

    partial void OnPluginTypeChanged(PluginType value) => _pluginCollectionView.View?.Refresh();

    partial void OnFilterTextChanged(string value) => _pluginCollectionView.View?.Refresh();

    [RelayCommand]
    private void Market() => Process.Start(new ProcessStartInfo { FileName = "https://stranslate.zggsong.com/plugins.html", UseShellExecute = true });

    [RelayCommand]
    private async Task AddPluginAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = _i18n.GetTranslation("SelectPluginFile"),
            Filter = "Spkg File (*.spkg)|*.spkg",
            Multiselect = true,
            RestoreDirectory = true
        };
        if (dialog.ShowDialog() != true) return;

        await InstallPluginsAsync(dialog.FileNames);
    }

    [RelayCommand]
    private async Task InstallPluginsAsync(DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;

        if (e.Data.GetData(DataFormats.FileDrop) is not string[] files) return;

        var spkgFiles = files.Where(f => f.EndsWith(".spkg", StringComparison.OrdinalIgnoreCase)).ToList();
        if (spkgFiles.Count == 0)
        {
            _snackbar.ShowError(_i18n.GetTranslation("NoValidPluginFile"));
            return;
        }

        await InstallPluginsAsync(spkgFiles);
    }

    private async Task InstallPluginsAsync(IEnumerable<string> files)
    {
        var needRestart = false;
        foreach (var spkgPluginFilePath in files)
        {
            var installResult = _pluginService.InstallPlugin(spkgPluginFilePath);

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
                    if (_pluginService.UpgradePlugin(installResult.ExistingPlugin, spkgPluginFilePath))
                    {
                        needRestart = true;
                        _snackbar.ShowSuccess(_i18n.GetTranslation("PluginInstallSuccess"));
                    }
                    else
                    {
                        _snackbar.ShowError(_i18n.GetTranslation("PluginUpgradeFailed"));
                    }
                }
            }
            else if (!installResult.Succeeded)
            {
                await new ContentDialog
                {
                    Title = _i18n.GetTranslation("PluginInstallFailed"),
                    CloseButtonText = _i18n.GetTranslation("Ok"),
                    DefaultButton = ContentDialogButton.Close,
                    Content = installResult.Message
                }.ShowAsync();
            }
            else
            {
                _snackbar.ShowSuccess(_i18n.GetTranslation("PluginInstallSuccess"));
            }
        }

        if (needRestart)
        {
            var restartResult = await new ContentDialog
            {
                Title = _i18n.GetTranslation("Prompt"),
                Content = _i18n.GetTranslation("PluginUpgradeSuccess"),
                PrimaryButtonText = _i18n.GetTranslation("RestartNow"),
                CloseButtonText = _i18n.GetTranslation("RestartLater"),
                DefaultButton = ContentDialogButton.Primary,
            }.ShowAsync();

            if (restartResult == ContentDialogResult.Primary)
            {
                UACHelper.Run(_settings.StartMode);
                App.Current.Shutdown();
            }
        }
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

        if (!_pluginService.UninstallPlugin(plugin))
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

public enum PluginType
{
    All,
    Translate,
    Ocr,
    Tts,
    Vocabulary,
}