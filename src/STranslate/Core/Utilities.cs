using Gma.System.MouseKeyHook;
using iNKORE.UI.WPF.Modern.Controls;
using STranslate.Plugin;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using Windows.Win32;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.Memory;
using WindowsInput;

namespace STranslate.Core;

public class Utilities
{
    #region StringUtils

    /// <summary>
    ///     自动识别语种
    /// </summary>
    /// <param name="text">输入语言</param>
    /// <param name="scale">英文占比</param>
    /// <returns>
    ///     Item1: SourceLang
    ///     Item2: TargetLang
    /// </returns>
    public static (LangEnum SourceLang, LangEnum TargetLang) AutomaticLanguageRecognition(string text, double scale = 0.8)
    {
        //1. 首先去除所有数字、标点及特殊符号
        //https://www.techiedelight.com/zh/strip-punctuations-from-a-string-in-csharp/
        text = Regex
            .Replace(text, "[1234567890!\"#$%&'()*+,-./:;<=>?@\\[\\]^_`{|}~，。、《》？；‘’：“”【】、{}|·！@#￥%……&*（）——+~\\\\]",
                string.Empty)
            .Replace(Environment.NewLine, "")
            .Replace(" ", "");

        //2. 取出上一步中所有英文字符
        var engStr = ExtractEngString(text);

        var ratio = (double)engStr.Length / text.Length;

        //3. 判断英文字符个数占第一步所有字符个数比例，若超过一定比例则判定原字符串为英文字符串，否则为中文字符串
        return ratio > scale
            ? (LangEnum.English, LangEnum.ChineseSimplified)
            : (LangEnum.ChineseSimplified, LangEnum.English);
    }

    /// <summary>
    ///     提取英文
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static string ExtractEngString(string str)
    {
        var regex = new Regex("[a-zA-Z]+");

        var matchCollection = regex.Matches(str);
        var ret = string.Empty;
        foreach (Match mMatch in matchCollection) ret += mMatch.Value;
        return ret;
    }

    public static string LinebreakHandler(string text, LineBreakHandleType type)
        => type switch
        {
            LineBreakHandleType.RemoveExtraLineBreak => NormalizeText(text),
            LineBreakHandleType.RemoveAllLineBreak => text.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " "),
            LineBreakHandleType.RemoveAllLineBreakWithoutSpace => text.Replace("\r\n", "").Replace("\n", "").Replace("\r", ""),
            _ => text,
        };

    /// <summary>
    /// 规范化给定的文本，通过移除或替换某些字符和模式。
    /// <see href="https://github1s.com/CopyTranslator/CopyTranslator/blob/master/src/common/translate/helper.ts#L172"/>
    /// </summary>
    /// <param name="text">要规范的源文本。</param>
    /// <returns>规范化后的文本。</returns>
    public static string NormalizeText(string text)
    {
        // 将所有的回车换行符替换为换行符
        text = text.Replace("\r\n", "\n");
        // 将所有的回车符替换为换行符
        text = text.Replace("\r", "\n");
        // 将所有的连字符换行符组合替换为空字符串
        text = text.Replace("-\n", "");

        // 遍历每个正则表达式模式，并进行替换
        text = Patterns.Aggregate(text, (current, pattern) => pattern.Replace(current, "#$1#"));

        // 将所有的换行符替换为空格
        text = text.Replace("\n", " ");
        // 使用sentenceEnds正则表达式进行替换
        text = SentenceEnds.Replace(text, "$1\n");

        // 返回处理后的字符串
        return text;
    }

    /// <summary>
    /// 文本框处理后可以Ctrl+Z撤销
    /// <see href="https://stackoverflow.com/questions/4476282/how-can-i-undo-a-textboxs-text-changes-caused-by-a-binding"/>
    /// </summary>
    /// <param name="textBox">需要处理的文本框。</param>
    /// <param name="transform">转换规则。</param>
    /// <param name="action">执行后动作。</param>
    public static void TransformText(TextBox textBox, Func<string, string> transform, Action? action = default)
    {
        var text = textBox.SelectedText.Length > 0 ? textBox.SelectedText : textBox.Text;

        var result = transform(text);
        if (result == text) return;

        if (textBox.SelectedText.Length == 0)
        {
            textBox.SelectAll();
        }

        textBox.SelectedText = result;

        action?.Invoke();

        textBox.Focus();
    }

    // 定义两个正则表达式模式列表，一个用于英文标点，一个用于中文标点
    private static readonly List<Regex> Patterns =
    [
        new(@"([?!.])[ ]?\n"), // 匹配英文标点符号后跟随换行符
        new(@"([？！。])[ ]?\n")
    ];
    // 定义一个正则表达式，用于匹配特定标点符号并用换行符替换
    private static readonly Regex SentenceEnds = new(@"#([?？！!.。])#");

    #endregion

    #region Microsoft Authentication

    /// <summary>
    ///     https://github.com/d4n3436/GTranslate/blob/master/src/GTranslate/Translators/MicrosoftTranslator.cs
    /// </summary>
    /// <param name="url"></param>
    /// <returns></returns>
    public static string GetSignature(string url)
    {
        string guid = Guid.NewGuid().ToString("N");
        string escapedUrl = Uri.EscapeDataString(url);
        string dateTime = DateTimeOffset.UtcNow.ToString("ddd, dd MMM yyyy HH:mm:ssG\\MT", CultureInfo.InvariantCulture);

        byte[] bytes = Encoding.UTF8.GetBytes($"MSTranslatorAndroidApp{escapedUrl}{dateTime}{guid}".ToLowerInvariant());

        using var hmac = new HMACSHA256(PrivateKey);
        byte[] hash = hmac.ComputeHash(bytes);

        return $"MSTranslatorAndroidApp::{Convert.ToBase64String(hash)}::{dateTime}::{guid}";
    }

    private static readonly byte[] PrivateKey =
    [
        0xa2, 0x29, 0x3a, 0x3d, 0xd0, 0xdd, 0x32, 0x73,
        0x97, 0x7a, 0x64, 0xdb, 0xc2, 0xf3, 0x27, 0xf5,
        0xd7, 0xbf, 0x87, 0xd9, 0x45, 0x9d, 0xf0, 0x5a,
        0x09, 0x66, 0xc6, 0x30, 0xc6, 0x6a, 0xaa, 0x84,
        0x9a, 0x41, 0xaa, 0x94, 0x3a, 0xa8, 0xd5, 0x1a,
        0x6e, 0x4d, 0xaa, 0xc9, 0xa3, 0x70, 0x12, 0x35,
        0xc7, 0xeb, 0x12, 0xf6, 0xe8, 0x23, 0x07, 0x9e,
        0x47, 0x10, 0x95, 0x91, 0x88, 0x55, 0xd8, 0x17
    ];

    #endregion

    #region ClipboardUtils

    #region Core

    private static readonly InputSimulator _inputSimulator = new();

    /// <summary>
    ///     使用 SendInput API 模拟 Ctrl+C 或 Ctrl+V 键盘输入。
    /// </summary>
    /// <param name="isCopy">如果为 true，则模拟 Ctrl+C；否则模拟 Ctrl+V。</param>
    public static void SendCtrlCV(bool isCopy = true)
    {
        // 先清理可能存在的按键状态 ！！！很重要否则模拟复制会失败
        //ReleaseModifierKeys();
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.CONTROL);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LCONTROL);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.RCONTROL);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.MENU);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LMENU);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.RMENU);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LWIN);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.RWIN);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.SHIFT);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.LSHIFT);
        _inputSimulator.Keyboard.KeyUp(VirtualKeyCode.RSHIFT);

        if (isCopy)
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_C);
        else
            _inputSimulator.Keyboard.ModifiedKeyStroke(VirtualKeyCode.CONTROL, VirtualKeyCode.VK_V);
    }

    /// <summary>
    ///     获取当前选中的文本。
    /// </summary>
    /// <param name="timeout">超时时间（以毫秒为单位），默认2000ms</param>
    /// <param name="cancellation">可以用来取消工作的取消标记</param>
    /// <returns>返回当前选中的文本。</returns>
    public static async Task<string?> GetSelectedTextAsync(int timeout = 2000, CancellationToken cancellation = default)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellation);
        cts.CancelAfter(timeout);

        try
        {
            return await GetSelectedTextImplAsync(timeout);
        }
        catch (OperationCanceledException)
        {
            return GetText()?.Trim(); // 超时时返回当前剪贴板内容
        }
    }

    /// <summary>
    ///     获取选中文本实现
    /// </summary>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <returns>返回当前选中的文本</returns>
    private static async Task<string?> GetSelectedTextImplAsync(int timeout = 2000)
    {
        var clipboardBackup = CreateClipboardBackup();

        try
        {
            var originalText = GetText();
            uint originalSequence = PInvoke.GetClipboardSequenceNumber();

            // 发送复制命令
            SendCtrlCV();

            var startTime = Environment.TickCount;
            var hasSequenceChanged = false;

            while (Environment.TickCount - startTime < timeout)
            {
                uint currentSequence = PInvoke.GetClipboardSequenceNumber();

                // 检查序列号是否变化
                if (currentSequence != originalSequence)
                {
                    hasSequenceChanged = true;
                    // 序列号变化后，等待一段时间确保内容完全更新
                    await Task.Delay(30);
                    break;
                }

                await Task.Delay(10);
            }

            var currentText = GetText();

            // 如果序列号变化了，或者内容发生了变化，或者原本就没有内容
            if (hasSequenceChanged ||
                !string.IsNullOrEmpty(currentText) ||
                currentText != originalText)
            {
                return currentText?.Trim();
            }

            return default; // 没有检测到变化
        }
        catch
        {
            return default;
        }
        finally
        {
            await RestoreClipboardAsync(clipboardBackup);
        }
    }

    #region Clipboard Backup

    private const nuint MAX_SINGLE_FORMAT_SIZE = 5 * 1024 * 1024; // 单个格式5MB限制
    private const nuint MAX_TOTAL_BACKUP_SIZE = 10 * 1024 * 1024; // 总备份10MB限制

    // 已知的位图/图像格式ID
    private static readonly uint[] ImageFormats =
    [
        2,   // CF_BITMAP - 位图句柄(不能直接备份)
        8,   // CF_DIB - 设备独立位图
        17,  // CF_DIBV5
        14,  // CF_PALETTE
    ];
    /// <summary>
    /// 创建剪贴板备份
    /// </summary>
    private static unsafe ClipboardBackup? CreateClipboardBackup()
    {
        try
        {
            TryOpenClipboard();

            var backup = new ClipboardBackup();
            nuint totalSize = 0;

            // 枚举剪贴板中所有实际存在的格式
            uint format = 0;
            while ((format = PInvoke.EnumClipboardFormats(format)) != 0)
            {
                // 🔹 跳过已知的图像格式
                if (ImageFormats.Contains(format))
                {
                    continue;
                }

                var handle = PInvoke.GetClipboardData(format);
                if (handle.IsNull)
                {
                    // 延迟渲染的格式,跳过
                    continue;
                }

                nuint size;
                try
                {
                    size = PInvoke.GlobalSize(new HGLOBAL(handle.Value));
                }
                catch
                {
                    // 🔹 某些格式可能无法获取大小,跳过
                    continue;
                }

                if (size == 0 || size > MAX_SINGLE_FORMAT_SIZE)
                {
                    // 空数据或超大数据,跳过
                    continue;
                }

                // 🔹 检查总备份大小限制
                if (totalSize + size > MAX_TOTAL_BACKUP_SIZE)
                {
                    break; // 停止备份,避免内存占用过大
                }

                void* pointer = null;
                try
                {
                    pointer = PInvoke.GlobalLock(new HGLOBAL(handle.Value));
                    if (pointer == null)
                    {
                        continue;
                    }

                    var buffer = new byte[size];
                    Marshal.Copy((IntPtr)pointer, buffer, 0, (int)size);
                    backup.FormatData[format] = buffer;
                    totalSize += size;
                }
                catch
                {
                    // 🔹 锁定或复制失败,跳过此格式
                    continue;
                }
                finally
                {
                    if (pointer != null)
                    {
                        try
                        {
                            PInvoke.GlobalUnlock(new HGLOBAL(handle.Value));
                        }
                        catch
                        {
                            // 忽略解锁失败
                        }
                    }
                }
            }

            PInvoke.CloseClipboard();
            return backup;
        }
        catch
        {
            try { PInvoke.CloseClipboard(); } catch { }
            return default;
        }
    }

    /// <summary>
    /// 恢复剪贴板内容
    /// </summary>
    private static async Task RestoreClipboardAsync(ClipboardBackup? backup)
    {
        if (backup?.FormatData == null || backup.FormatData.Count == 0)
            return;

        try
        {
            await Task.Run(() =>
            {
                TryOpenClipboard();
                PInvoke.EmptyClipboard();

                // 按照备份时的顺序恢复所有格式
                foreach (var (format, data) in backup.FormatData)
                {
                    try
                    {
                        RestoreClipboardFormat(format, data);
                    }
                    catch
                    {
                        // 🔹 某些格式恢复失败,继续处理其他格式
                        continue;
                    }
                }

                PInvoke.CloseClipboard();
            });
        }
        catch
        {
            try { PInvoke.CloseClipboard(); } catch { }
        }
    }

    /// <summary>
    /// 恢复特定格式的剪贴板数据
    /// </summary>
    private static unsafe void RestoreClipboardFormat(uint format, byte[] data)
    {
        var hGlobal = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)data.Length);
        if (hGlobal.IsNull) return;

        try
        {
            var target = PInvoke.GlobalLock(hGlobal);
            if (target != null)
            {
                try
                {
                    Marshal.Copy(data, 0, (IntPtr)target, data.Length);
                }
                finally
                {
                    PInvoke.GlobalUnlock(hGlobal);
                }
            }

            PInvoke.SetClipboardData(format, new HANDLE(hGlobal.Value));
            hGlobal = default; // 防止在finally中释放
        }
        finally
        {
            if (!hGlobal.IsNull)
                PInvoke.GlobalFree(hGlobal);
        }
    }

    /// <summary>
    /// 剪贴板备份数据结构
    /// </summary>
    private class ClipboardBackup
    {
        public Dictionary<uint, byte[]> FormatData { get; } = new();
    }

    #endregion

    #endregion

    #region TextCopy

    private static readonly uint[] SupportedFormats =
    [
        CF_UNICODETEXT,
        CF_TEXT,
        CF_OEMTEXT,
        CustomFormat1,
        CustomFormat2,
        CustomFormat3,
        CustomFormat4,
        CustomFormat5,
    ];

    private const uint CF_TEXT = 1; // ANSI 文本
    private const uint CF_UNICODETEXT = 13; // Unicode 文本
    private const uint CF_OEMTEXT = 7; // OEM 文本
    private const uint CF_DIB = 16; // 位图（保留常量但不参与文本读取）
    private const uint CustomFormat1 = 49499; // 自定义格式 1
    private const uint CustomFormat2 = 49290; // 自定义格式 2
    private const uint CustomFormat3 = 49504; // 自定义格式 3
    private const uint CustomFormat4 = 50103; // 自定义格式 4
    private const uint CustomFormat5 = 50104; // 自定义格式 5

    // https://github.com/CopyText/TextCopy/blob/main/src/TextCopy/WindowsClipboard.cs

    public static void SetText(string text)
    {
        TryOpenClipboard();

        InnerSet(text);
    }

    private static unsafe void InnerSet(string text)
    {
        PInvoke.EmptyClipboard();
        HGLOBAL hGlobal = default;
        try
        {
            var bytes = (text.Length + 1) * 2;
            hGlobal = PInvoke.GlobalAlloc(GLOBAL_ALLOC_FLAGS.GMEM_MOVEABLE, (nuint)bytes);

            if (hGlobal.IsNull) throw new Win32Exception(Marshal.GetLastWin32Error());

            var target = PInvoke.GlobalLock(hGlobal);

            if (target == null) throw new Win32Exception(Marshal.GetLastWin32Error());

            try
            {
                var textBytes = Encoding.Unicode.GetBytes(text + '\0');
                Marshal.Copy(textBytes, 0, (IntPtr)target, textBytes.Length);
            }
            finally
            {
                PInvoke.GlobalUnlock(hGlobal);
            }

            // 修复：直接传递 hGlobal.Value（IntPtr）而不是 HGLOBAL
            if (PInvoke.SetClipboardData(CF_UNICODETEXT, new HANDLE(hGlobal.Value)).IsNull) throw new Win32Exception(Marshal.GetLastWin32Error());

            hGlobal = default;
        }
        finally
        {
            if (!hGlobal.IsNull) PInvoke.GlobalFree(hGlobal);

            PInvoke.CloseClipboard();
        }
    }

    private static void TryOpenClipboard()
    {
        var num = 10;
        while (true)
        {
            if (PInvoke.OpenClipboard(default)) break;

            if (--num == 0) throw new Win32Exception(Marshal.GetLastWin32Error());

            Thread.Sleep(100);
        }
    }

    public static string? GetText()
    {
        // 先占有剪贴板，再检查可用格式，减少 TOCTTOU 竞态
        TryOpenClipboard();

        var support = SupportedFormats.Any(format => PInvoke.IsClipboardFormatAvailable(format));
        if (!support)
        {
            PInvoke.CloseClipboard();
            return null;
        }

        return InnerGet();
    }

    private static Encoding GetOemEncoding()
    {
        try
        {
            // 使用真实 OEM 代码页；不可用时回退到系统默认编码
            var cp = (int)PInvoke.GetOEMCP();
            return Encoding.GetEncoding(cp);
        }
        catch
        {
            return Encoding.Default;
        }
    }

    private static unsafe string? InnerGet()
    {
        HANDLE handle = default;
        void* pointer = null;

        try
        {
            foreach (var format in SupportedFormats)
            {
                handle = PInvoke.GetClipboardData(format);
                if (handle.IsNull) continue;

                pointer = PInvoke.GlobalLock(new HGLOBAL(handle.Value));
                if (pointer == null) continue;

                var size = PInvoke.GlobalSize(new HGLOBAL(handle.Value));
                if (size <= 0)
                {
                    // 修复：避免锁泄漏
                    PInvoke.GlobalUnlock(new HGLOBAL(handle.Value));
                    pointer = null;
                    continue;
                }

                var buffer = new byte[size];
                Marshal.Copy((IntPtr)pointer, buffer, 0, (int)size);

                // 仅对文本/自定义文本格式做解码
                var encoding = format switch
                {
                    CF_UNICODETEXT => Encoding.Unicode, // UTF-16LE
                    CF_TEXT => Encoding.Default,        // ANSI（系统ACP）
                    CF_OEMTEXT => GetOemEncoding(),     // OEM（可进一步改为 OEM 代码页，见下备注）
                    _ => Encoding.UTF8                  // 自定义格式按 UTF-8 尝试
                };

                var result = encoding.GetString(buffer);
                var nullCharIndex = result.IndexOf('\0');
                return nullCharIndex == -1 ? result : result[..nullCharIndex];
            }
        }
        finally
        {
            if (pointer != null) PInvoke.GlobalUnlock(new HGLOBAL(handle.Value));
            PInvoke.CloseClipboard();
        }

        return null;
    }

    #endregion

    #endregion

    #region MouseHookUtils

    private static IKeyboardMouseEvents? _mouseHook;
    private static bool _isMouseListening;
    private static string _oldText = string.Empty;

    /// <summary>
    /// 鼠标划词文本选择事件
    /// </summary>
    public static event Action<string>? MouseTextSelected;

    /// <summary>
    /// 启动鼠标划词监听
    /// </summary>
    public static async Task StartMouseTextSelectionAsync()
    {
        if (_isMouseListening) return;

        _mouseHook = Hook.GlobalEvents();
        _mouseHook.MouseDragStarted += OnDragStarted;
        _mouseHook.MouseDragFinished += OnDragFinished;

        _isMouseListening = true;

        // 等待钩子启动
        await Task.Delay(100);
    }

    /// <summary>
    /// 停止鼠标划词监听
    /// </summary>
    public static void StopMouseTextSelection()
    {
        if (!_isMouseListening) return;

        _isMouseListening = false;

        if (_mouseHook != null)
        {
            _mouseHook.MouseDragStarted -= OnDragStarted;
            _mouseHook.MouseDragFinished -= OnDragFinished;
            _mouseHook.Dispose();
            _mouseHook = null;
        }
    }

    /// <summary>
    /// 切换鼠标划词监听状态
    /// </summary>
    public static async Task ToggleMouseTextSelection()
    {
        if (_isMouseListening)
        {
            StopMouseTextSelection();
        }
        else
        {
            await StartMouseTextSelectionAsync();
        }
    }

    /// <summary>
    /// 获取鼠标划词监听状态
    /// </summary>
    public static bool IsMouseTextSelectionListening => _isMouseListening;

    private static void OnDragStarted(object? sender, System.Windows.Forms.MouseEventArgs e)
        => _oldText = GetText() ?? string.Empty;

    private static void OnDragFinished(object? sender, System.Windows.Forms.MouseEventArgs e)
    {
        if (e.Button == System.Windows.Forms.MouseButtons.Left)
        {
            // 异步处理文本获取和事件触发
            _ = Task.Run(async () =>
            {
                // 异步获取选中文本
                var selectedText = await GetSelectedTextAsync();
                if (!string.IsNullOrEmpty(selectedText) && selectedText != _oldText)
                {
                    MouseTextSelected?.Invoke(selectedText);
                }
            });
        }
    }

    #endregion

    #region WindowUtils

    public static FrameworkElement? FindSettingElementByContent(DependencyObject? parent, string content)
    {
        if (parent == null) return null;

        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i) as FrameworkElement;
            if (child != null)
            {
                switch (child)
                {
                    case SettingsCard settingsCard when
                    (settingsCard.Header is string header && header.Equals(content, StringComparison.OrdinalIgnoreCase)) ||
                    (settingsCard.Description is string description && description.Equals(content, StringComparison.OrdinalIgnoreCase)):
                        return settingsCard;

                    case SettingsExpander settingsExpander when
                    (settingsExpander.Header is string expanderHeader && expanderHeader.Equals(content, StringComparison.OrdinalIgnoreCase)) ||
                    (settingsExpander.Description is string expanderDescription && expanderDescription.Equals(content, StringComparison.OrdinalIgnoreCase)):
                        return settingsExpander;
                }

                (child as Expander)?.IsExpanded = true;
            }

            var result = FindSettingElementByContent(child, content);
            if (result != null)
            {
                return result;
            }
        }
        return null;
    }

    public static void BringIntoViewAndHighlight(FrameworkElement element)
    {
        element.BringIntoView();

        if (element is SettingsExpander settingsExpander)
        {
            // iNKORE.UI.WPF.Modern 中 背景色在名为ExpanderHeader 的 ToggleButton上设定，没有取Template Background
            var expanderHeader = FindVisualChild<ToggleButton>(settingsExpander, "ExpanderHeader");
            if (expanderHeader != null)
            {
                element = expanderHeader;
            }
        }

        // 获取element的背景色存储为brush
        var originalBrush = element.GetValue(Panel.BackgroundProperty) as SolidColorBrush;

        var highlightColor = (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#40808080");
        var transparentColor = Colors.Transparent;

        var animation = new ColorAnimationUsingKeyFrames
        {
            Duration = new Duration(TimeSpan.FromSeconds(1.3)),
            FillBehavior = FillBehavior.Stop // 动画结束后恢复原样
        };
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.0))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.2))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.4))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.6))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.8))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(transparentColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.0))));
        animation.KeyFrames.Add(new DiscreteColorKeyFrame(highlightColor, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.2))));

        var brush = new SolidColorBrush(transparentColor);
        // 将背景设置为动画画笔
        element.SetCurrentValue(Panel.BackgroundProperty, brush);

        // 动画结束后，将背景属性设置为 null 以恢复默认值
        animation.Completed += (s, e) =>
        {
            element.SetCurrentValue(Panel.BackgroundProperty, originalBrush);
        };

        brush.BeginAnimation(SolidColorBrush.ColorProperty, animation);
    }

    public static T? FindVisualChild<T>(DependencyObject? parent, string? childName = null) where T : FrameworkElement
    {
        if (parent == null) return null;

        T? foundChild = null;

        var childrenCount = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < childrenCount; i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is not T childType)
            {
                foundChild = FindVisualChild<T>(child, childName);
                if (foundChild != null) break;
            }
            else if (!string.IsNullOrEmpty(childName))
            {
                if (childType.Name == childName)
                {
                    foundChild = childType;
                    break;
                }
            }
            else
            {
                foundChild = childType;
                break;
            }
        }

        return foundChild;
    }

    #endregion

    #region BitmapUtils

    public static BitmapImage ToBitmapImage(Bitmap bitmap, ImageFormat? imageFormat = default)
    {
        using var memory = new MemoryStream();
        imageFormat ??= ImageFormat.Png;    // 默认使用 PNG 格式
        bitmap.Save(memory, imageFormat);
        memory.Position = 0;

        var bitmapImage = new BitmapImage();
        bitmapImage.BeginInit();
        bitmapImage.StreamSource = memory;
        bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
        bitmapImage.EndInit();
        bitmapImage.Freeze();

        return bitmapImage;
    }

    public static BitmapImage ToBitmapImage(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);

        var img = new BitmapImage();
        img.BeginInit();
        img.StreamSource = stream;
        img.CacheOption = BitmapCacheOption.OnLoad;
        img.EndInit();
        img.Freeze();
        return img;
    }

    public static Bitmap ToBitmap(BitmapSource bitmapSource, BitmapEncoder? encoder = default)
    {
        // 规范化 BitmapSource 到标准格式
        var formatConvertedBitmap = new FormatConvertedBitmap(bitmapSource, PixelFormats.Bgr24, null, 0);

        encoder ??= new PngBitmapEncoder(); // 默认使用 PNG 编码器
        encoder.Frames.Add(BitmapFrame.Create(formatConvertedBitmap));
        using var stream = new MemoryStream();
        encoder.Save(stream);
        stream.Position = 0;

        // 创建一个新的Bitmap，它会复制数据而不依赖于流
        using var originalBitmap = new Bitmap(stream);
        return new Bitmap(originalBitmap);
    }

    public static Bitmap ToBitmap(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return new Bitmap(stream);
    }

    public static byte[] ToBytes(BitmapSource bitmapSource, BitmapEncoder? encoder = default)
    {
        encoder ??= new PngBitmapEncoder(); // 默认使用 PNG 编码器
        encoder.Frames.Add(BitmapFrame.Create(bitmapSource));

        using var stream = new MemoryStream();
        encoder.Save(stream);
        return stream.ToArray();
    }

    public static byte[] ToBytes(Bitmap bitmap, ImageFormat? imageFormat = default)
    {
        imageFormat ??= ImageFormat.Png; // 默认使用 PNG 格式
        using var stream = new MemoryStream();
        bitmap.Save(stream, imageFormat);
        return stream.ToArray();
    }

    public static bool IsImageFile(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".png", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".bmp", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".gif", StringComparison.OrdinalIgnoreCase) ||
               extension.Equals(".webp", StringComparison.OrdinalIgnoreCase);
    }

    public static byte[] ToBase64Utf8Bytes(byte[] bytes)
    {
        var base64String = Convert.ToBase64String(bytes);
        return Encoding.UTF8.GetBytes(base64String);
    }

    public static byte[] ToBase64Utf8BytesFast(byte[] bytes)
    {
        var base64Length = ((bytes.Length + 2) / 3) * 4;
        var base64Chars = base64Length <= 1024
            ? stackalloc char[base64Length]
            : new char[base64Length];

        Convert.TryToBase64Chars(bytes, base64Chars, out _);
        return Encoding.UTF8.GetBytes(base64Chars.ToArray());
    }

    /// <summary>
    ///     图像变成背景
    /// </summary>
    /// <param name="bmp"></param>
    /// <returns></returns>
    public static ImageBrush ToImageBrush(Bitmap bmp)
    {
        var hBitmap = bmp.GetHbitmap();
        try
        {
            var bitmapSource = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions()
            );

            bitmapSource.Freeze();

            var brush = new ImageBrush { ImageSource = bitmapSource };
            brush.Freeze();

            return brush;
        }
        finally
        {
            // 释放 GDI 对象以防止内存泄漏
            if (hBitmap != IntPtr.Zero)
                PInvoke.DeleteObject(new HGDIOBJ(hBitmap));
        }
    }

    #endregion

    #region ProcessUtils

    public static bool IsMultiInstance()
    {
        var runningProcesses = Process.GetProcessesByName(Constant.AppName);
        return runningProcesses.Length > 1;
    }

    /// <summary>
    /// 执行外部程序
    /// </summary>
    /// <param name="filename">程序文件名或路径</param>
    /// <param name="args">参数数组</param>
    /// <param name="useAdmin">是否以管理员权限运行</param>
    /// <param name="wait">是否等待程序执行完成</param>
    /// <param name="timeout">超时时间（毫秒），仅在wait=true时有效</param>
    /// <returns>执行结果，包含是否成功和退出代码</returns>
    public static (bool Success, int? ExitCode) ExecuteProgram(
        string filename,
        string[] args,
        bool useAdmin = false,
        bool wait = false,
        int timeout = 30000)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return (false, null);

        try
        {
            // 使用 StringBuilder 优化字符串拼接
            var argumentsBuilder = new StringBuilder();
            foreach (var arg in args)
            {
                if (argumentsBuilder.Length > 0)
                    argumentsBuilder.Append(' ');

                // 只有包含空格或特殊字符时才添加引号
                if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\t'))
                {
                    argumentsBuilder.Append('"')
                        .Append(arg.Replace("\"", "\\\""))
                        .Append('"');
                }
                else
                {
                    argumentsBuilder.Append(arg);
                }
            }

            var processStartInfo = new ProcessStartInfo(filename, argumentsBuilder.ToString())
            {
                UseShellExecute = useAdmin, // 管理员权限需要使用Shell执行
                CreateNoWindow = true,
                RedirectStandardError = !useAdmin,  // 管理员模式下不能重定向
                RedirectStandardOutput = !useAdmin
            };

            if (useAdmin)
            {
                processStartInfo.Verb = "runas";
            }

            using var process = new Process { StartInfo = processStartInfo };

            if (!process.Start())
                return (false, null);

            if (wait)
            {
                var completed = process.WaitForExit(timeout);
                if (!completed)
                {
                    // 超时后尝试终止进程
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (InvalidOperationException)
                    {
                        // 进程可能已经退出
                    }
                    return (false, null);
                }

                return (true, process.ExitCode);
            }

            return (true, null);
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223) // 用户取消UAC
        {
            return (false, null);
        }
        catch (Exception)
        {
            return (false, null);
        }
    }

    /// <summary>
    /// 执行外部程序的异步版本
    /// </summary>
    /// <param name="filename">程序文件名或路径</param>
    /// <param name="args">参数数组</param>
    /// <param name="useAdmin">是否以管理员权限运行</param>
    /// <param name="timeout">超时时间（毫秒）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>执行结果，包含是否成功、退出代码和输出</returns>
    public static async Task<(bool Success, int? ExitCode, string? Output, string? Error)> ExecuteProgramAsync(
        string filename,
        string[] args,
        bool useAdmin = false,
        int timeout = 30000,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(filename))
            return (false, null, null, null);

        try
        {
            var argumentsBuilder = new StringBuilder();
            foreach (var arg in args)
            {
                if (argumentsBuilder.Length > 0)
                    argumentsBuilder.Append(' ');

                if (arg.Contains(' ') || arg.Contains('"') || arg.Contains('\t'))
                {
                    argumentsBuilder.Append('"')
                        .Append(arg.Replace("\"", "\\\""))
                        .Append('"');
                }
                else
                {
                    argumentsBuilder.Append(arg);
                }
            }

            var processStartInfo = new ProcessStartInfo(filename, argumentsBuilder.ToString())
            {
                UseShellExecute = useAdmin,
                CreateNoWindow = true,
                RedirectStandardError = !useAdmin,
                RedirectStandardOutput = !useAdmin
            };

            if (useAdmin)
            {
                processStartInfo.Verb = "runas";
            }

            using var process = new Process { StartInfo = processStartInfo };

            if (!process.Start())
                return (false, null, null, null);

            if (useAdmin)
            {
                // 管理员模式下无法读取输出，只等待完成
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                    return (true, process.ExitCode, null, null);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (InvalidOperationException) { }
                    return (false, null, null, null);
                }
            }
            else
            {
                // 非管理员模式下可以读取输出
                var outputTask = process.StandardOutput.ReadToEndAsync();
                var errorTask = process.StandardError.ReadToEndAsync();

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(timeout);

                try
                {
                    await process.WaitForExitAsync(cts.Token);
                    var output = await outputTask;
                    var error = await errorTask;

                    return (true, process.ExitCode, output, error);
                }
                catch (OperationCanceledException)
                {
                    try
                    {
                        if (!process.HasExited)
                            process.Kill();
                    }
                    catch (InvalidOperationException) { }
                    return (false, null, null, null);
                }
            }
        }
        catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
        {
            return (false, null, null, null);
        }
        catch (Exception)
        {
            return (false, null, null, null);
        }
    }

    #endregion

    #region ShortcutUtils

    /// <summary>
    ///     设置开机自启
    /// </summary>
    public static void SetStartup()
    {
        ShortCutCreate();
    }

    /// <summary>
    ///     检查是否已经设置开机自启
    /// </summary>
    /// <returns>true: 开机自启 false: 非开机自启</returns>
    public static bool IsStartup()
    {
        return ShortCutExist(DataLocation.AppExePath, DataLocation.StartupPath);
    }

    /// <summary>
    ///     取消开机自启
    /// </summary>
    public static void UnSetStartup()
    {
        ShortCutDelete(DataLocation.AppExePath, DataLocation.StartupPath);
    }

    /// <summary>
    ///     设置桌面快捷方式
    /// </summary>
    public static void SetDesktopShortcut()
    {
        ShortCutCreate(true);
    }

    #region Private Method

    /// <summary>
    ///     获取指定文件夹下的所有快捷方式（不包括子文件夹）
    /// </summary>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static List<string> GetDirectoryFileList(string target)
    {
        if (!Directory.Exists(target))
            return [];

        return [.. Directory.GetFiles(target, "*.lnk")];
    }

    /// <summary>
    ///     判断快捷方式是否存在
    /// </summary>
    /// <param name="path">快捷方式目标（可执行文件的绝对路径）</param>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static bool ShortCutExist(string path, string target)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (string.IsNullOrWhiteSpace(target))
            throw new ArgumentException("Target cannot be null or empty", nameof(target));

        if (!Directory.Exists(target))
            return false;

        var list = GetDirectoryFileList(target);
        return list.Any(item => path.Equals(GetAppPathViaShortCut(item), StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    ///     删除快捷方式（通过快捷方式目标进行删除）
    /// </summary>
    /// <param name="path">快捷方式目标（可执行文件的绝对路径）</param>
    /// <param name="target">目标文件夹（绝对路径）</param>
    /// <returns></returns>
    private static bool ShortCutDelete(string path, string target)
    {
        var result = false;
        var list = GetDirectoryFileList(target);
        foreach (var item in list.Where(item => path == GetAppPathViaShortCut(item)))
        {
            File.Delete(item);
            result = true;
        }

        return result;
    }

    /// <summary>
    ///     为本程序创建一个快捷方式
    /// </summary>
    /// <param name="isDesktop">是否为桌面快捷方式</param>
    /// <returns></returns>
    private static bool ShortCutCreate(bool isDesktop = false)
    {
        var result = true;
        try
        {
            var shortcutPath = isDesktop ? DataLocation.DesktopShortcutPath : DataLocation.StartupShortcutPath;
            CreateShortcut(shortcutPath, DataLocation.AppExePath, DataLocation.AppExePath);
        }
        catch
        {
            result = false;
        }

        return result;
    }

    #region 非 COM 实现快捷键创建

    /// <see href="https://blog.csdn.net/weixin_42288222/article/details/124150046" />
    /// <summary>
    ///     获取快捷方式中的目标（可执行文件的绝对路径）
    /// </summary>
    /// <param name="shortCutPath">快捷方式的绝对路径</param>
    /// <returns></returns>
    private static string? GetAppPathViaShortCut(string shortCutPath)
    {
        try
        {
            // ReSharper disable once SuspiciousTypeConversion.Global
            var file = (IShellLink)new ShellLink();
            try
            {
                file.Load(shortCutPath, 2);
                var sb = new StringBuilder(256);
                file.GetPath(sb, sb.Capacity, IntPtr.Zero, 2);
                return sb.ToString();
            }
            finally
            {
                // 释放COM对象
                if (file != null && Marshal.IsComObject(file))
                {
                    Marshal.ReleaseComObject(file);
                }
            }
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    ///     向目标路径创建指定文件的快捷方式
    /// </summary>
    /// <param name="shortcutPath">快捷方式路径</param>
    /// <param name="appPath">App路径</param>
    /// <param name="description">提示信息</param>
    private static void CreateShortcut(string shortcutPath, string appPath, string description)
    {
        // ReSharper disable once SuspiciousTypeConversion.Global
        var link = (IShellLink)new ShellLink();
        link.SetDescription(description);
        link.SetPath(appPath);
        var workingDir = Directory.GetParent(appPath)?.FullName;
        if (workingDir != null)
            link.SetWorkingDirectory(workingDir);

        if (File.Exists(shortcutPath))
            File.Delete(shortcutPath);
        link.Save(shortcutPath, false);
    }

    [ComImport]
    [Guid("00021401-0000-0000-C000-000000000046")]
    internal class ShellLink
    {
    }

    [ComImport]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    internal interface IShellLink : IPersistFile
    {
        void GetPath([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd,
            int fFlags);

        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void GetArguments([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);

        void GetIconLocation([Out][MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath,
            out int piIcon);

        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, int dwReserved);
        void Resolve(IntPtr hwnd, int fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    #endregion

    #endregion

    #endregion
}