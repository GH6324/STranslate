# 插件系统

## 插件加载机制 (`Core/PluginManager.cs`)

### 插件来源

插件从两个目录加载：

| 目录 | 说明 | 位置 |
|------|------|------|
| `PreinstalledDirectory` | 内置插件 | `Plugins/` 文件夹 |
| `PluginsDirectory` | 用户安装的插件 | `%APPDATA%\STranslate\Plugins\` |

### 插件包格式

每个插件是一个 `.spkg` 文件（ZIP 压缩包），包含：
```
plugin.json          # 元数据
YourPlugin.dll       # 主程序集
icon.png            # 可选图标
Languages/*.xaml     # 可选 i18n 文件
```

### 插件类型

插件实现 `IPlugin` 接口及其子类型：

| 接口 | 功能 | 基类 |
|------|------|------|
| `ITranslatePlugin` | 翻译服务 | `TranslatePluginBase` / `LlmTranslatePluginBase` |
| `IOcrPlugin` | OCR 服务 | `OcrPluginBase` |
| `ITtsPlugin` | 文本转语音 | `TtsPluginBase` |
| `IVocabularyPlugin` | 词汇管理 | `VocabularyPluginBase` |
| `IDictionaryPlugin` | 字典查询 | - |

### 插件生命周期

```
PluginManager.LoadPlugins()
→ 扫描插件目录（PreinstalledDirectory + PluginsDirectory）
→ 发现 .spkg 文件
→ 从 plugin.json 提取元数据
→ 通过 PluginAssemblyLoader 加载程序集
→ 使用 MetadataLoadContext 反射查找 IPlugin 实现
→ 创建带类型信息的 PluginMetaData
```

### 程序集加载

- 使用 `PluginAssemblyLoader` 和 `System.Reflection.MetadataLoadContext`
- 支持插件隔离加载
- 避免程序集冲突

## 插件元数据 (`PluginMetaData`)

```csharp
public class PluginMetaData
{
    public string PluginID { get; set; }      // 唯一标识符
    public string Name { get; set; }          // 显示名称
    public string Author { get; set; }        // 作者
    public string Version { get; set; }       // 版本
    public string Description { get; set; }   // 描述
    public string Website { get; set; }       // 网站
    public string ExecuteFileName { get; set; } // DLL 文件名
    public string IconPath { get; set; }      // 图标路径
    public Type PluginType { get; set; }      // 插件类型（ITranslatePlugin/IOcrPlugin 等）
}
```
