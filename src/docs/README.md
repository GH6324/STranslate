# STranslate 文档

本文档是 CLAUDE.md 的拆分版本，按功能模块组织，便于查找和维护。

## 文档结构

```
docs/
├── README.md                      # 本文档
├── 01-overview.md                 # 项目概述、主要功能
├── 02-build.md                    # 构建与开发命令
├── 03-architecture/               # 架构设计
│   ├── README.md                  # 架构概览
│   ├── startup.md                 # 启动流程
│   ├── plugin-system.md           # 插件系统
│   ├── service-management.md      # 服务管理
│   ├── key-interfaces.md          # 关键接口
│   └── data-flow.md               # 数据流（翻译示例）
├── 04-features/                   # 功能特性
│   ├── README.md                  # 功能特性概览
│   ├── hotkey.md                  # 热键系统
│   └── clipboard-monitor.md       # 剪贴板监听
├── 05-storage/                    # 存储与配置
│   └── settings.md                # 设置与存储
├── 06-plugin/                     # 插件开发
│   ├── README.md                  # 插件开发指南
│   ├── plugin-format.md           # 插件包格式
│   └── third-party.md             # 社区插件开发
├── 07-development/                # 开发任务
│   └── common-tasks.md            # 常见开发任务
├── 08-reference/                  # 参考文档
│   ├── important-files.md         # 重要文件
│   └── dependencies.md            # 关键依赖
└── 09-notes/                      # 注意事项
    └── claude-notes.md            # 给 Claude 的注意事项
```

## 快速导航

| 我想了解... | 查看文档 |
|------------|---------|
| 项目是什么 | [项目概述](01-overview.md) |
| 如何构建运行 | [构建与开发](02-build.md) |
| 系统如何工作 | [架构概览](03-architecture/README.md) |
| 热键系统 | [热键系统](04-features/hotkey.md) |
| 剪贴板监听 | [剪贴板监听](04-features/clipboard-monitor.md) |
| 如何开发插件 | [插件开发指南](06-plugin/README.md) → [社区插件开发](06-plugin/third-party.md) |
| 修改核心服务 | [常见开发任务](07-development/common-tasks.md) |
| 查找关键文件 | [重要文件](08-reference/important-files.md) |

## 主索引

返回主索引文件：[../CLAUDE.md](../CLAUDE.md)
