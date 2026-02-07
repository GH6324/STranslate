# 数据流：翻译示例

本章节以翻译功能为例，说明数据如何在系统中流动。

## 完整数据流图

```
用户触发翻译（快捷键/UI）
    ↓
MainWindowViewModel.TranslateCommand
    ↓
翻译准备阶段
    ├── 取消进行中的操作
    ├── 重置所有服务状态（ResetAllServices()）
    └── 语言检测（LanguageDetector.GetLanguageAsync()）
    ↓
获取激活服务
    └── TranslateService 返回 ExecutionMode.Automatic 的启用服务
    ↓
并行执行翻译（ExecuteTranslationForServicesAsync）
    ├── 使用 SemaphoreSlim 限制并发数（ProcessorCount * 10）
    └── 对每个 Service 调用 ExecuteTranslationHandlerAsync
    ↓
插件执行（ExecuteAsync）
    ├── plugin.Reset()
    ├── plugin.TransResult.IsProcessing = true
    └── await plugin.TranslateAsync(...)
    ↓
插件内部处理
    ├── Context.LoadSettingStorage<Settings>() 获取配置
    ├── Context.HttpService 发起 HTTP 请求（支持代理）
    └── 解析响应，调用 result.Success(text) 或 result.Fail(message)
    ↓
结果处理
    ├── TranslateResult 是 ObservableObject，自动更新 UI
    ├── 如启用回译，调用 ExecuteBackAsync()
    └── 保存到历史数据库（SqlService）
```

## 详细步骤

### 1. 触发翻译

用户通过以下方式触发翻译：
- 全局热键（如 `Alt + D` 划词翻译）
- 主界面输入框 + 翻译按钮
- 剪贴板监听自动触发

入口：`MainWindowViewModel.TranslateCommand`

### 2. 翻译准备

```csharp
// 取消之前的翻译任务
_cancellationTokenSource?.Cancel();
_cancellationTokenSource = new CancellationTokenSource();

// 重置所有服务状态
ResetAllServices();

// 检测源语言（如设置为自动）
var detectedLanguage = await LanguageDetector.GetLanguageAsync(inputText);
```

### 3. 获取激活服务

```csharp
// 获取所有启用的翻译服务
var services = TranslateService.GetServices()
    .Where(s => s.Options.IsEnabled && s.Options.ExecutionMode == ExecutionMode.Automatic);
```

### 4. 并行执行

```csharp
// 限制并发数，避免过多请求
var semaphore = new SemaphoreSlim(Environment.ProcessorCount * 10);

var tasks = services.Select(async service =>
{
    await semaphore.WaitAsync(cancellationToken);
    try
    {
        await ExecuteTranslationHandlerAsync(service, request, cancellationToken);
    }
    finally
    {
        semaphore.Release();
    }
});

await Task.WhenAll(tasks);
```

### 5. 插件执行

```csharp
// 在 Service 类中
public async Task ExecuteAsync(TranslateRequest request, CancellationToken cancellationToken)
{
    // 重置插件状态
    Plugin.Reset();

    // 标记为处理中
    Plugin.TransResult.IsProcessing = true;

    // 执行翻译
    await Plugin.TranslateAsync(request, Plugin.TransResult, cancellationToken);
}
```

### 6. 插件内部处理

```csharp
public class MyTranslatePlugin : TranslatePluginBase
{
    public override async Task TranslateAsync(
        TranslateRequest request,
        TranslateResult result,
        CancellationToken cancellationToken)
    {
        // 1. 加载设置（API Key 等）
        var settings = Context.LoadSettingStorage<Settings>();

        // 2. 构建请求
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, settings.ApiUrl);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(new { text = request.Text }),
            Encoding.UTF8, "application/json");

        // 3. 发送请求（自动使用系统代理）
        var response = await Context.HttpService.SendAsync(
            httpRequest, cancellationToken);

        // 4. 解析响应
        var content = await response.Content.ReadAsStringAsync();
        var data = JsonSerializer.Deserialize<Response>(content);

        // 5. 设置结果（自动更新 UI）
        if (data.Success)
            result.Success(data.TranslatedText);
        else
            result.Fail(data.ErrorMessage);
    }
}
```

### 7. 结果处理

- `TranslateResult` 继承自 `ObservableObject`，结果变更自动通知 UI
- 如果启用回译，对目标文本再次调用翻译（方向相反）
- 保存到 SQLite 历史数据库：`%APPDATA%\STranslate\Cache\history.db`

## 线程安全

- 翻译请求使用 `SemaphoreSlim` 控制并发（默认 `ProcessorCount * 10`）
- 所有插件操作支持 `CancellationToken` 取消
- 每个插件实例是独立的，线程安全由各自实现保证
