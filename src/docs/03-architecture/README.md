# 架构概览

STranslate 采用插件化架构，核心设计思想是将功能拆分为可独立开发、部署和扩展的插件。

## 核心组件

### 1. 应用程序启动 (`App.xaml.cs:296-319`)
- 通过 `SingleInstance<App>` 强制单实例
- Velopack 更新检查
- 设置加载（Settings, HotkeySettings, ServiceSettings）
- DI 容器设置（Microsoft.Extensions.Hosting）
- 通过 `PluginManager` 加载插件
- 通过 `ServiceManager` 初始化服务

### 2. 插件系统 (`Core/PluginManager.cs`)
- 插件从两个目录加载：
  - `PreinstalledDirectory`: 内置插件，在 `Plugins/` 文件夹
  - `PluginsDirectory`: 用户安装的插件，在数据目录
- 每个插件是一个 `.spkg` 文件（ZIP 压缩包），包含：
  - `plugin.json` - 元数据
  - 插件 DLL
  - 可选资源（图标、语言文件）
- 插件实现 `IPlugin` 接口及其子类型：
  - `ITranslatePlugin` - 翻译服务
  - `IOcrPlugin` - OCR 服务
  - `ITtsPlugin` - 文本转语音
  - `IVocabularyPlugin` - 词汇管理
  - `IDictionaryPlugin` - 字典查询

### 3. 服务管理 (`Core/ServiceManager.cs`)
- `Service` 是包装插件的运行时实例
- 服务从 `PluginMetaData` 创建并存储在 `ServiceData`（持久化配置）中
- 四种服务类型：翻译、OCR、TTS、词汇
- 服务在启动时从设置和插件元数据加载

## 子模块文档

- [启动流程](startup.md) - 详细启动流程
- [插件系统](plugin-system.md) - 插件加载与管理机制
- [服务管理](service-management.md) - Service 与 Plugin 的关系
- [关键接口](key-interfaces.md) - IPlugin、IPluginContext 等接口定义
- [数据流](data-flow.md) - 以翻译为例的数据流
