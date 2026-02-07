# 服务管理

## Service 与 Plugin 的关系

理解 **Service** 和 **Plugin** 的区别至关重要：

| 概念 | 说明 | 类比 |
|------|------|------|
| **Plugin (`PluginMetaData`)** | 插件的类型定义，包含程序集信息、元数据 | 类（Class） |
| **Service** | 插件的运行时实例，拥有独立的配置、状态和服务 ID | 对象实例（Object） |

### 关键区别

- 同一 **Plugin** 类型可被多个 **Service** 共享使用
- 多个 **Service** 可使用同一 **Plugin** 类型（如两个不同 API Key 的百度翻译服务）
- 每个 **Service** 拥有独立的 `ServiceID`（GUID）和配置

## Service 创建流程

`ServiceManager.CreateService()` 的工作流程：

```
1. 克隆 PluginMetaData（每个 Service 有自己的副本）
2. 创建或重用 ServiceID（GUID）
3. 通过 Activator.CreateInstance() 创建插件实例
4. 创建 PluginContext 提供给插件
5. 组装 Service 对象，包含：
   - Plugin（插件实例）
   - MetaData（元数据副本）
   - Context（插件上下文）
   - Options（服务选项）
```

## 服务加载流程

```
ServiceManager.LoadServices()
→ 遍历 ServiceSettings 中的每个 ServiceData
→ 与 PluginMetaData 匹配（通过 PluginID）
→ 如果匹配成功，创建 Service 实例
→ 调用 Service.Initialize()
   → Plugin.Init(IPluginContext)
      → 插件获取上下文，初始化自身
```

## Service 数据结构

```csharp
public class Service
{
    public IPlugin Plugin { get; }           // 插件实例
    public PluginMetaData MetaData { get; }  // 元数据
    public IPluginContext Context { get; }   // 插件上下文
    public ServiceOptions Options { get; }   // 服务选项（启用、自动执行等）
    public Guid ServiceID { get; }           // 服务唯一标识
}
```

## 服务类型

四种核心服务类型：

| 服务类型 | 管理器 | 接口 |
|---------|--------|------|
| 翻译 | `TranslateService` | `ITranslatePlugin` |
| OCR | `OcrService` | `IOcrPlugin` |
| TTS | `TtsService` | `ITtsPlugin` |
| 词汇 | `VocabularyService` | `IVocabularyPlugin` |
