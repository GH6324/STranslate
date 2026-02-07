# 插件开发指南

STranslate 支持通过插件扩展功能。本指南介绍插件开发的基础知识。

## 插件类型

| 插件类型 | 基类 | 主要接口 | 说明 |
|---------|------|---------|------|
| 翻译 | `TranslatePluginBase` / `LlmTranslatePluginBase` | `ITranslatePlugin` | 支持 LLM 的使用 `LlmTranslatePluginBase` |
| OCR | `OcrPluginBase` | `IOcrPlugin` | 图像识别 |
| TTS | `TtsPluginBase` | `ITtsPlugin` | 文本转语音 |
| 词汇 | `VocabularyPluginBase` | `IVocabularyPlugin` | 单词查询/管理 |

## 插件基类说明

- `TranslatePluginBase`: 提供常用功能如语言映射、结果处理、HTTP 请求辅助
- `LlmTranslatePluginBase`: 继承自 `TranslatePluginBase`，专为 LLM 服务设计，提供提示词编辑、流式响应处理
- 插件基类位于 `STranslate.Plugin` 项目中，插件项目需引用该包

## 子模块文档

- [插件包格式](plugin-format.md) - .spkg 格式、plugin.json 规范
- [社区插件开发](third-party.md) - 第三方插件开发流程、调试、发布

## 快速开始

1. 选择插件类型（翻译/OCR/TTS/词汇）
2. 基于现有官方插件模板创建项目
3. 实现对应接口
4. 打包为 .spkg 文件
5. 安装测试
