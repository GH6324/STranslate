# 剪贴板监听功能

剪贴板监听功能允许应用程序在后台监视系统剪贴板的变化，当检测到文本内容时自动触发翻译。

## 实现架构

### 核心组件 (`Helpers/ClipboardMonitor.cs`)

- 使用 Win32 API `AddClipboardFormatListener` / `RemoveClipboardFormatListener` 注册剪贴板监听
- 通过 `HwndSource` 在 WPF 窗口上挂接 `WndProc` 接收 `WM_CLIPBOARDUPDATE` 消息
- 使用 CsWin32 PInvoke 生成类型安全的 Win32 API 绑定

```csharp
public class ClipboardMonitor : IDisposable
{
    private HwndSource? _hwndSource;
    private HWND _hwnd;
    private string _lastText = string.Empty;

    public event Action<string>? OnClipboardTextChanged;

    public void Start()
    {
        // 使用 WindowInteropHelper 获取窗口句柄
        var windowHelper = new WindowInteropHelper(_window);
        _hwnd = new HWND(windowHelper.Handle);
        _hwndSource = HwndSource.FromHwnd(windowHelper.Handle);
        _hwndSource?.AddHook(WndProc);
        PInvoke.AddClipboardFormatListener(_hwnd);
    }

    private nint WndProc(nint hwnd, int msg, nint wParam, nint lParam, ref bool handled)
    {
        if (msg == PInvoke.WM_CLIPBOARDUPDATE)
        {
            _ = Task.Run(async () =>
            {
                await Task.Delay(100);  // 延迟确保剪贴板数据已完全写入
                var text = ClipboardHelper.GetText();
                if (!string.IsNullOrWhiteSpace(text) && text != _lastText)
                {
                    _lastText = text;
                    OnClipboardTextChanged?.Invoke(text);
                    _lastText = string.Empty;  // 触发后重置，允许相同内容再次触发
                }
            });
            handled = true;
        }
        return nint.Zero;
    }
}
```

## 控制方式

1. **全局热键**: `Alt + Shift + A`（默认）- 在任何地方切换监听状态
2. **主窗口按钮**: HeaderControl 中的切换按钮，带状态指示（IsOn/IsOff）
3. **设置项**: `Settings.IsClipboardMonitorVisible` 控制按钮是否显示

## 状态通知

开启/关闭状态通过 Windows 托盘通知（Toast Notification）提示用户，因为此时主窗口可能处于隐藏状态。

## 实现细节

### 延迟处理

使用 `await Task.Delay(100)` 延迟 100ms 确保剪贴板数据已完全写入，避免读取到空或不完整的数据。

### 重复触发处理

- 使用 `_lastText` 字段记录上一次触发内容
- 触发后重置为空字符串，允许相同内容再次触发（用户可能再次复制相同内容）

### 线程安全

剪贴板操作在后台线程执行，避免阻塞 UI 线程：

```csharp
_ = Task.Run(async () =
{
    // 剪贴板操作
});
```

## 相关文件

| 文件 | 用途 |
|------|---------|
| `STranslate/Helpers/ClipboardMonitor.cs` | 剪贴板监听实现（Win32 API） |
| `STranslate/Views/HeaderControl.xaml` | 主窗口标题栏按钮 |
