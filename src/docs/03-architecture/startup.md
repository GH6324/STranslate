# 核心架构流程 - 启动流程

## 应用程序启动流程 (`App.xaml.cs:296-319`)

```
App.OnStartup()
├── SingleInstance<App> 强制单实例
├── Velopack 更新检查
├── 设置加载
│   ├── Settings.json - 通用设置
│   ├── HotkeySettings.json - 快捷键配置
│   └── ServiceSettings.json - 服务配置
├── DI 容器设置（Microsoft.Extensions.Hosting）
├── PluginManager.LoadPlugins() - 加载插件
└── ServiceManager.LoadServices() - 初始化服务
```

### 详细步骤

1. **强制单实例**
   - 使用 `SingleInstance<App>` 确保只有一个应用实例运行

2. **Velopack 更新检查**
   - 检查应用更新
   - 自动下载并安装更新（如有）

3. **设置加载**
   - `Settings` - 通用应用程序设置
   - `HotkeySettings` - 热键配置，包含全局热键和软件内热键
   - `ServiceSettings` - 服务配置（启用、顺序、选项）

4. **DI 容器设置**
   - 使用 Microsoft.Extensions.Hosting
   - 注册核心服务（TranslateService, OcrService, TtsService, VocabularyService）
   - 注册视图模型

5. **插件加载**
   - `PluginManager.LoadPlugins()` 扫描插件目录
   - 从 `plugin.json` 提取元数据
   - 通过 `PluginAssemblyLoader` 加载程序集
   - 查找 `IPlugin` 实现
   - 创建带类型信息的 `PluginMetaData`

6. **服务初始化**
   - `ServiceManager.LoadServices()` 遍历设置中的每个 `ServiceData`
   - 与 `PluginMetaData` 匹配
   - 创建 `Service` 实例
   - 调用 `Service.Initialize()` → `Plugin.Init(IPluginContext)`
