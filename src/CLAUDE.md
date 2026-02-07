# CLAUDE.md

本文档为 Claude Code (claude.ai/code) 在处理本仓库代码时提供指导。

## 文档导航

本文档已按功能模块拆分为以下子文档：

### 快速开始
- [**项目概述**](docs/01-overview.md) - STranslate 项目简介、主要功能
- [**构建与开发**](docs/02-build.md) - 构建命令、项目结构

### 架构设计
- [**架构概览**](docs/03-architecture/README.md) - 核心架构说明
  - [启动流程](docs/03-architecture/startup.md) - 应用程序启动过程
  - [插件系统](docs/03-architecture/plugin-system.md) - 插件加载与管理
  - [服务管理](docs/03-architecture/service-management.md) - Service 与 Plugin 的关系
  - [关键接口](docs/03-architecture/key-interfaces.md) - IPlugin、IPluginContext 等接口定义
  - [数据流](docs/03-architecture/data-flow.md) - 翻译功能的数据流示例

### 功能特性
- [**功能特性概览**](docs/04-features/README.md)
  - [热键系统](docs/04-features/hotkey.md) - 全局热键、软件内热键、特殊热键功能
  - [剪贴板监听](docs/04-features/clipboard-monitor.md) - 剪贴板监听实现

### 存储与配置
- [**设置与存储**](docs/05-storage/settings.md) - 设置架构、存储位置

### 插件开发
- [**插件开发指南**](docs/06-plugin/README.md)
  - [插件包格式](docs/06-plugin/plugin-format.md) - .spkg 格式、plugin.json
  - [社区插件开发](docs/06-plugin/third-party.md) - 第三方插件开发流程

### 开发参考
- [**常见开发任务**](docs/07-development/common-tasks.md) - 修改核心服务、UI 更改、调试插件
- [**重要文件**](docs/08-reference/important-files.md) - 关键文件索引
- [**关键依赖**](docs/08-reference/dependencies.md) - 技术栈与依赖项
- [**给 Claude 的注意事项**](docs/09-notes/claude-notes.md) - 项目特定注意事项

## 快速参考

| 任务 | 相关文档 |
|------|---------|
| 了解项目结构 | [项目概述](docs/01-overview.md) |
| 构建项目 | [构建与开发](docs/02-build.md) |
| 开发插件 | [插件开发指南](docs/06-plugin/README.md) → [社区插件开发](docs/06-plugin/third-party.md) |
| 修改热键功能 | [热键系统](docs/04-features/hotkey.md) |
| 修改剪贴板监听 | [剪贴板监听](docs/04-features/clipboard-monitor.md) |
| 修改核心服务 | [常见开发任务](docs/07-development/common-tasks.md) |
| 查找关键文件 | [重要文件](docs/08-reference/important-files.md) |
