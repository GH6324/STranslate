# 关键依赖

## 框架与运行时

| 依赖 | 用途 |
|------|------|
| **.NET 10.0-windows** | WPF 应用程序框架 |

## MVVM 与架构

| 依赖 | 用途 |
|------|------|
| **CommunityToolkit.Mvvm** | MVVM 模式支持（源生成器） |
| **Microsoft.Extensions.*** | 依赖注入、配置、日志等 |

## UI 组件

| 依赖 | 用途 |
|------|------|
| **iNKORE.UI.WPF.Modern** | 现代 UI 控件和主题 |

## 日志

| 依赖 | 用途 |
|------|------|
| **Serilog** | 结构化日志记录 |

## 热键与输入

| 依赖 | 用途 |
|------|------|
| **NHotkey.Wpf** | 全局热键注册 |
| **MouseKeyHook** | 鼠标键盘钩子（Ctrl+CC 等功能） |
| **ChefKeys** | Win 键热键支持 |

## 网络

| 依赖 | 用途 |
|------|------|
| **System.Net.Http** | HTTP 请求（支持代理） |

## 存储

| 依赖 | 用途 |
|------|------|
| **Microsoft.Data.Sqlite** | SQLite 数据库（历史记录） |

## 更新

| 依赖 | 用途 |
|------|------|
| **Velopack** | 自动更新框架 |

## 插件加载

| 依赖 | 用途 |
|------|------|
| **System.Reflection.MetadataLoadContext** | 插件程序集反射加载 |

## IL 织入

| 依赖 | 用途 |
|------|------|
| **Costura.Fody** | 程序集合并 |
| **MethodBoundaryAspect.Fody** | AOP 面向切面编程 |

## Win32 API

| 依赖 | 用途 |
|------|------|
| **Microsoft.Windows.CsWin32** | 类型安全的 P/Invoke |
