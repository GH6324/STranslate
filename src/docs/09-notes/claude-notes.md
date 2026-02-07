# 给 Claude 的注意事项

## 平台限制

- 这是一个 **仅限 Windows 的 WPF 应用程序**（使用 Windows 特定 API）
- 不支持跨平台（Linux/macOS）

## 插件系统

- 插件在运行时从单独的 DLL **动态加载**
- 所有插件接口都在 `STranslate.Plugin` 项目中，与主应用程序共享
- 插件实例**按服务创建**（非单例）
- 使用 `IPluginContext` 获取插件功能（不要直接传递应用程序服务）
- 预安装插件 ID 定义在 `Constant.cs:56-74`

## 存储与配置

- 设置使用**原子写入**和备份文件（.tmp + .bak）
- 应用程序支持**便携模式**（创建 `PortableConfig/` 文件夹）
- 预安装插件在 `Plugins/` 并复制到输出
- 用户插件位于 `%APPDATA%\STranslate\Plugins\`

## 线程安全

- 翻译请求使用 `SemaphoreSlim` 控制并发（默认 `ProcessorCount * 10`）
- 所有插件操作支持 `CancellationToken` 取消
- 插件程序集加载使用 `PluginAssemblyLoader` 和 `System.Reflection.MetadataLoadContext`
- 服务被包装在 `Service` 类中，包含 `Plugin`, `MetaData`, `Context` 和 `Options`

## 插件基类

- 翻译插件可以扩展 `TranslatePluginBase` 或 `LlmTranslatePluginBase` 以获得 LLM 功能
- 应用程序使用 Fody 织入器（Costura.Fody 用于程序集合并，MethodBoundaryAspect.Fody 用于 AOP）

## 剪贴板监听

- 使用 Win32 API `AddClipboardFormatListener`
- 监听状态通过 Windows Toast 通知反馈
- 因为主窗口可能处于隐藏状态，所以使用通知而非弹窗

## 开发模式

- Debug 模式下使用开发版图标
- 日志文件位于 `%APPDATA%\STranslate\Logs\{Version}\`
- 插件调试可输出到主程序 Plugins 目录
