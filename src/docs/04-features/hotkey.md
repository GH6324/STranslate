# 热键系统

热键系统支持**全局热键**（系统级，即使应用未聚焦也能触发）和**软件内热键**（仅在应用聚焦时生效）。

## 热键类型

| 类型 | 说明 | 使用场景 |
|------|------|----------|
| `GlobalHotkey` | 全局热键，通过 NHotkey.Wpf 注册 | 打开窗口、截图翻译、划词翻译等 |
| `Hotkey` | 软件内热键，通过 WPF KeyBinding | 窗口内快捷键如 Ctrl+B 自动翻译 |
| 按住触发键 | 通过低级别键盘钩子实现 | 按住特定键时临时激活功能 |

## 热键数据结构 (`Core/HotkeyModel.cs`)

```csharp
public record struct HotkeyModel
{
    public bool Alt { get; set; }
    public bool Shift { get; set; }
    public bool Win { get; set; }
    public bool Ctrl { get; set; }
    public Key CharKey { get; set; } = Key.None;

    // 转换为 ModifierKeys 用于 NHotkey 注册
    public readonly ModifierKeys ModifierKeys { get; }

    // 从字符串解析（如 "Ctrl + Alt + T"）
    public HotkeyModel(string hotkeyString)

    // 验证热键有效性
    public bool Validate(bool validateKeyGestrue = false)
}
```

## 热键设置 (`Core/HotkeySettings.cs`)

```csharp
public partial class HotkeySettings : ObservableObject
{
    // 全局热键
    public GlobalHotkey OpenWindowHotkey { get; set; } = new("Alt + G");
    public GlobalHotkey InputTranslateHotkey { get; set; } = new("None");
    public GlobalHotkey CrosswordTranslateHotkey { get; set; } = new("Alt + D");
    public GlobalHotkey ScreenshotTranslateHotkey { get; set; } = new("Alt + S");
    public GlobalHotkey ClipboardMonitorHotkey { get; set; } = new("Alt + Shift + A");  // 剪贴板监听开关
    public GlobalHotkey OcrHotkey { get; set; } = new("Alt + Shift + S");
    // ... 其他全局热键

    // 软件内热键 - MainWindow
    public Hotkey OpenSettingsHotkey { get; set; } = new("Ctrl + OemComma");
    public Hotkey AutoTranslateHotkey { get; set; } = new("Ctrl + B");
    // ... 其他软件内热键
}
```

## 全局热键注册 (`Helpers/HotkeyMapper.cs`)

全局热键注册使用两种机制：

### 1. NHotkey.Wpf（标准热键）

```csharp
internal static bool SetHotkey(HotkeyModel hotkey, Action action)
{
    HotkeyManager.Current.AddOrReplace(
        hotkeyStr,
        hotkey.CharKey,
        hotkey.ModifierKeys,
        (_, _) => action.Invoke()
    );
}
```

### 2. ChefKeys（Win 键专用）

```csharp
// LWin/RWin 需要使用 ChefKeys 库
if (hotkeyStr is "LWin" or "RWin")
    return SetWithChefKeys(hotkeyStr, action);
```

### 3. 低级别键盘钩子（按住触发）

```csharp
// 使用 SetWindowsHookEx(WH_KEYBOARD_LL) 实现全局按键监听
public static void StartGlobalKeyboardMonitoring()
{
    _hookProc = HookCallback;
    _hookHandle = PInvoke.SetWindowsHookEx(
        WINDOWS_HOOK_ID.WH_KEYBOARD_LL,
        _hookProc,
        hModule,
        0
    );
}
```

## 热键注册流程

```
App.OnStartup()
→ _hotkeySettings.LazyInitialize()
   → ApplyCtrlCc()              // 启用/禁用 Ctrl+CC 划词
   → ApplyIncrementalTranslate() // 启用/禁用增量翻译按键
   → RegisterHotkeys()          // 注册所有全局热键
      → HotkeyMapper.SetHotkey() // 每个热键调用 NHotkey
```

## 全屏检测与热键屏蔽

```csharp
private Action WithFullscreenCheck(Action action)
{
    return () =>
    {
        if (settings.IgnoreHotkeysOnFullscreen &&
            Win32Helper.IsForegroundWindowFullscreen())
            return;  // 全屏时忽略热键

        action();
    };
}
```

## 托盘图标状态

热键状态通过托盘图标反映（优先级从高到低）：

| 状态 | 图标 | 说明 |
|------|------|------|
| `NoHotkey` | 禁用热键图标 | 全局热键被禁用 (`DisableGlobalHotkeys=true`) |
| `IgnoreOnFullScreen` | 全屏忽略图标 | 全屏时忽略热键 (`IgnoreHotkeysOnFullscreen=true`) |
| `Normal` | 正常图标 | 热键正常工作 |
| `Dev` | 开发版图标 | Debug 模式下的正常状态 |

## 热键冲突处理

```csharp
// 注册前检查热键是否可用
internal static bool CheckAvailability(HotkeyModel currentHotkey)
{
    try
    {
        HotkeyManager.Current.AddOrReplace("Test", key, modifiers, ...);
        return true;  // 可以注册
    }
    catch
    {
        return false; // 热键被占用
    }
}

// 冲突时标记并提示用户
GlobalHotkey.IsConflict = !HotkeyMapper.SetHotkey(...);
```

## 特殊热键功能

### 1. Ctrl+CC 划词翻译

- 监听 Ctrl 键状态，检测快速按两次 C 键
- 通过 `CtrlSameCHelper` 实现（使用 `MouseKeyHook` 库）
- 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

### 2. 按住触发键

- 注册按住键：按下时触发 `OnPress`，抬起时触发 `OnRelease`
- 用于增量翻译等功能
- 支持 `DisableGlobalHotkeys` 和 `IgnoreHotkeysOnFullscreen` 设置

### 3. 热键编辑控件 (`Controls/HotkeyControl.cs`)

- 自定义 WPF 控件用于热键设置界面
- 弹出对话框捕获按键输入
- 支持验证和冲突检测

## 相关文件

| 文件 | 用途 |
|------|---------|
| `STranslate/Core/HotkeySettings.cs` | 热键配置模型、热键注册管理 |
| `STranslate/Core/HotkeyModel.cs` | 热键数据结构、解析与验证 |
| `STranslate/Helpers/HotkeyMapper.cs` | 热键注册、低级别键盘钩子 |
| `STranslate/Controls/HotkeyControl.cs` | 热键设置自定义控件 |
| `STranslate/Controls/HotkeyDisplay.cs` | 热键显示自定义控件 |
| `STranslate/Views/Pages/HotkeyPage.xaml` | 热键设置页面 |
