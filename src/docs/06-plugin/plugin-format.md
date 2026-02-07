# 插件包格式 (.spkg)

`.spkg` 文件是 ZIP 压缩包，是 STranslate 插件的标准分发格式。

## 包结构

```
plugin.json          # 元数据（必需）
YourPlugin.dll       # 主程序集（必需）
icon.png            # 可选图标
Languages/          # 可选多语言文件目录
    ├── zh-cn.xaml
    ├── en.xaml
    └── ...
```

## plugin.json 规范

```json
{
  "PluginID": "unique-id",
  "Name": "Plugin Name",
  "Author": "Author",
  "Version": "1.0.0",
  "Description": "Description",
  "Website": "https://example.com",
  "ExecuteFileName": "YourPlugin.dll",
  "IconPath": "icon.png"
}
```

### 字段说明

| 字段 | 类型 | 必需 | 说明 |
|------|------|------|------|
| `PluginID` | string | 是 | 唯一标识符（32位十六进制GUID格式） |
| `Name` | string | 是 | 插件显示名称 |
| `Author` | string | 否 | 作者名称 |
| `Version` | string | 是 | 版本号（如 1.0.0） |
| `Description` | string | 否 | 插件描述 |
| `Website` | string | 否 | 项目网站 |
| `ExecuteFileName` | string | 是 | 主DLL文件名 |
| `IconPath` | string | 否 | 图标文件路径 |

### PluginID 生成

使用 GUID 生成器创建一个 32 位十六进制字符串：

```bash
# PowerShell
[Guid]::NewGuid().ToString("N")

# 输出示例：d99c702e39b44be5a9e49983ff0f4fff
```

**注意**：每个插件都需要重新生成唯一的 PluginID，重复可能导致插件无法使用。

## 创建 .spkg 文件

```powershell
# 将插件文件打包为 .spkg（ZIP 格式）
Compress-Archive -Path "plugin.json", "YourPlugin.dll", "icon.png", "Languages" -DestinationPath "YourPlugin.spkg"
```

## 安装插件

1. **通过 UI 安装**：设置 → 插件 → 安装 → 选择 .spkg 文件
2. **手动放置**：将 .spkg 文件放入 `%APPDATA%\STranslate\Plugins\` 目录
3. **预安装插件**：放在 `Plugins/` 目录，随应用一起分发
