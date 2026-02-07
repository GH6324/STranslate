# 关键接口

## IPlugin (基础接口)

所有插件必须实现的基础接口：

```csharp
public interface IPlugin : IDisposable
{
    // 使用上下文初始化插件
    void Init(IPluginContext context);

    // 返回设置界面 UserControl
    UserControl GetSettingUI();

    // 清理资源
    void Dispose();
}
```

## IPluginContext (提供给插件的上下文)

插件通过上下文访问主应用程序功能：

```csharp
public interface IPluginContext
{
    // 元数据
    PluginMetaData MetaData { get; }

    // 日志
    ILogger Logger { get; }

    // HTTP 服务（支持代理）
    IHttpService HttpService { get; }

    // 音频播放
    IAudioPlayer AudioPlayer { get; }

    // 提示消息
    ISnackbar Snackbar { get; }

    // 系统通知
    INotification Notification { get; }

    // 持久化存储（自动定位到插件专属目录）
    T LoadSettingStorage<T>() where T : class, new();
    void SaveSettingStorage<T>(T settings) where T : class;

    // i18n 支持
    string GetTranslation(string key);
}
```

## ITranslatePlugin (翻译插件)

```csharp
public interface ITranslatePlugin : IPlugin
{
    // 核心翻译方法
    Task TranslateAsync(
        TranslateRequest request,
        TranslateResult result,
        CancellationToken cancellationToken);

    // 语言映射
    string GetSourceLanguage(string language);
    string GetTargetLanguage(string language);

    // 结果属性（双向绑定）
    TranslateResult TransResult { get; set; }
    TranslateResult TransBackResult { get; set; }
}
```

## IOcrPlugin (OCR 插件)

```csharp
public interface IOcrPlugin : IPlugin
{
    // 图像转文本
    Task<OcrResult> RecognizeAsync(
        OcrRequest request,
        CancellationToken cancellationToken);

    // 支持的语言列表
    IReadOnlyList<string> SupportedLanguages { get; }
}
```

## ITtsPlugin (TTS 插件)

```csharp
public interface ITtsPlugin : IPlugin
{
    // 文本转语音
    Task SpeakAsync(
        TtsRequest request,
        CancellationToken cancellationToken);

    // 停止播放
    void Stop();
}
```

## IVocabularyPlugin (词汇插件)

```csharp
public interface IVocabularyPlugin : IPlugin
{
    // 保存单词到生词本
    Task SaveAsync(VocabularyEntry entry);

    // 查询生词本
    Task<IReadOnlyList<VocabularyEntry>> QueryAsync();
}
```

## 插件基类

### TranslatePluginBase

提供常用功能：
- 语言映射
- 结果处理
- HTTP 请求辅助

### LlmTranslatePluginBase

继承自 `TranslatePluginBase`，专为 LLM 服务设计：
- 提示词编辑
- 流式响应处理
- 对话历史管理
