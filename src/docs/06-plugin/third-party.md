# 社区插件开发指南 (ThirdPlugins)

## 标准开发流程

**核心原则：基于现有插件仓库修改**

### 1. 找到官方社区插件仓库

- 访问 GitHub: `https://github.com/STranslate/STranslate.Plugin.Translate.{Name}`
- 选择与你需求相似的插件类型（翻译/OCR/TTS/生词本）

### 2. Clone 仓库

```bash
# Clone 代码
git clone https://github.com/STranslate/STranslate.Plugin.Translate.DeepLX.git
cd STranslate.Plugin.Translate.DeepLX

# 创建自己的 git 仓库
rm -rf .git
git init .

# 重命名操作
# 修改项目文件、命名空间等
```

### 3. 修改必要信息

- `plugin.json`: 更新 PluginID、Name、Version、Description
- `*.csproj`: 更新项目名称、RepositoryUrl
- `Main.cs`: 修改核心逻辑
- `Settings.cs`: 修改配置模型
- `icon.png`: 替换图标

### 4. 在主项目中调试（可断点调试）

```powershell
# 1. 下载主项目代码
git clone https://github.com/STranslate/STranslate.git
cd STranslate

# 2. 将插件代码放到 Plugins/ThirdPlugins/ 目录
#    例如：Plugins/ThirdPlugins/STranslate.Plugin.Translate.YourPlugin/

# 3. 添加到解决方案
dotnet sln add Plugins/ThirdPlugins/STranslate.Plugin.Translate.YourPlugin/STranslate.Plugin.Translate.YourPlugin/STranslate.Plugin.Translate.YourPlugin.csproj

# 4. 在 Visual Studio 中
#    - 打开 STranslate.sln
#    - 设置 STranslate 为启动项目
#    - 配置 Debug 模式
#    - 右键插件项目 → 编译
#    - 启动调试（F5）
#    - 插件会自动加载，可设置断点
```

### 5. 版本管理与发布

```bash
# 1. 更新版本号（plugin.json）
# 2. 更新 CHANGELOG.md
# 3. 提交代码
git add .
git commit -m "feat: add your feature"

# 4. 打 tag（注意：tag 必须以 v 开头，后面跟版本号）
git tag v1.0.0
git push origin main
git push origin v1.0.0

# 5. GitHub Actions 自动构建并发布 Release
#    - 生成 .spkg 文件
#    - 创建 Release
#    - 上传 .spkg 作为附件
```

## 版本号与 Tag 规范

**重要规则：**
- `plugin.json` 中的 `"Version"` 必须与 Git Tag 一致
- Tag 格式：`v{版本号}`，例如：`v1.0.0`、`v1.2.3`
- 版本号更新后必须打新 Tag 才能触发自动发布

**示例：**
```json
// plugin.json
{
  "Version": "1.0.0",  // ← 这个版本号
  ...
}
```

```bash
# 对应的 Tag
git tag v1.0.0  # ← 必须以 v开头
git push origin v1.0.0
```

## 插件类型参考表

| 插件类型 | 基类 | 主要接口 | 说明 |
|---------|------|---------|------|
| 翻译 | `TranslatePluginBase` / `LlmTranslatePluginBase` | `ITranslatePlugin` | 支持 LLM 的使用 `LlmTranslatePluginBase` |
| OCR | `OcrPluginBase` | `IOcrPlugin` | 图像识别 |
| TTS | `TtsPluginBase` | `ITtsPlugin` | 文本转语音 |
| 词汇 | `VocabularyPluginBase` | `IVocabularyPlugin` | 单词查询/管理 |

## 已知社区插件示例

| 插件名称 | 类型 | 仓库地址 |
|---------|------|---------|
| DeepLX | 翻译 | `STranslate/STranslate.Plugin.Translate.DeepLX` |
| Gemini | 翻译 | `STranslate/STranslate.Plugin.Translate.Gemini` |
| 阿里云翻译 | 翻译 | `STranslate/STranslate.Plugin.Translate.Ali` |
| 通义千问 | 翻译 | `STranslate/STranslate.Plugin.Translate.QwenMt` |
| Google 网页翻译 | 翻译 | `STranslate/STranslate.Plugin.Translate.GoogleWebsite` |
| 必应词典 | 翻译 | `STranslate/STranslate.Plugin.Translate.BingDict` |
| Gemini OCR | OCR | `STranslate/STranslate.Plugin.Ocr.Gemini` |
| Paddle OCR | OCR | `STranslate/STranslate.Plugin.Ocr.Paddle` |
| 默默记单词生词本 | 词汇 | `STranslate/STranslate.Plugin.Vocabulary.Maimemo` |

## 常见问题

**Q: 如何获取插件的唯一 ID？**

A: 使用 GUID 生成器创建一个 32 位十六进制字符串，例如：`d99c702e39b44be5a9e49983ff0f4fff`，每个插件都需要重新生成，唯一ID重复可能会导致插件无法使用

**Q: 插件需要哪些必备文件？**

A: `plugin.json`, `Main.cs`, `Settings.cs`, `icon.png`, `.csproj`

**Q: 如何调试插件？**

A:
1. 设置 Debug 输出路径到主程序的 Plugins 目录
2. 启动主程序，插件会自动加载
3. 查看 `%APPDATA%\STranslate\Logs\` 中的日志

**Q: 插件版本如何管理？**

A:
1. 在 `plugin.json` 中更新 `Version`
2. 更新 `CHANGELOG.md`
3. 打 Tag: `git tag v1.0.0`
4. 推送: `git push origin main && git push origin v1.0.0`
5. GitHub Actions 自动构建并发布 Release

**Q: 如何支持多语言？**

A: 在 `Languages/` 目录添加 `.xaml` 和 `.json` 文件，通过 `IPluginContext.GetTranslation()` 获取
