# 重要文件

## 核心文件

| 文件 | 用途 |
|------|---------|
| `STranslate/App.xaml.cs` | 应用程序入口、DI 设置、生命周期 |
| `STranslate/Core/PluginManager.cs` | 插件发现、加载、安装 |
| `STranslate/Core/ServiceManager.cs` | 服务创建、生命周期 |
| `STranslate/Services/BaseService.cs` | 所有服务类型的基础 |

## 插件相关

| 文件 | 用途 |
|------|---------|
| `STranslate.Plugin/IPlugin.cs` | 核心插件接口 |
| `STranslate.Plugin/PluginMetaData.cs` | 插件元数据模型 |
| `STranslate.Plugin/Service.cs` | 运行时服务实例 |

## 热键相关

| 文件 | 用途 |
|------|---------|
| `STranslate/Core/HotkeySettings.cs` | 热键配置模型、热键注册管理 |
| `STranslate/Core/HotkeyModel.cs` | 热键数据结构、解析与验证 |
| `STranslate/Helpers/HotkeyMapper.cs` | 热键注册、低级别键盘钩子 |
| `STranslate/Controls/HotkeyControl.cs` | 热键设置自定义控件 |
| `STranslate/Controls/HotkeyDisplay.cs` | 热键显示自定义控件 |
| `STranslate/Views/Pages/HotkeyPage.xaml` | 热键设置页面 |

## 功能相关

| 文件 | 用途 |
|------|---------|
| `STranslate/Helpers/ClipboardMonitor.cs` | 剪贴板监听实现（Win32 API） |

## 构建与配置

| 文件 | 用途 |
|------|---------|
| `build.ps1` | Release 构建脚本 |
| `Directory.Packages.props` | 集中式 NuGet 版本 |
